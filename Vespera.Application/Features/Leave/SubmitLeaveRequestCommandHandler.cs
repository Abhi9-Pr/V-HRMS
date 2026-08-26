using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Leave;

public sealed class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Result<SubmitLeaveRequestResponse>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<LeaveType> _leaveTypes;
    private readonly IReadRepository<LeavePolicy> _leavePolicies;
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly IWriteRepository<LeaveRequest> _leaveRequestWriter;
    private readonly IReadRepository<LeaveBalance> _balances;
    private readonly IWriteRepository<LeaveBalance> _balanceWriter;
    private readonly IReadRepository<BlackoutPeriod> _blackouts;
    private readonly IReadRepository<Holiday> _holidays;
    private readonly IWriteRepository<ApprovalChain> _chainWriter;
    private readonly LeaveApprovalChainBuilder _chainBuilder;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SubmitLeaveRequestCommandHandler(
        IReadRepository<User> users, IReadRepository<Employee> employees, IReadRepository<LeaveType> leaveTypes,
        IReadRepository<LeavePolicy> leavePolicies, IReadRepository<LeaveRequest> leaveRequests,
        IWriteRepository<LeaveRequest> leaveRequestWriter, IReadRepository<LeaveBalance> balances,
        IWriteRepository<LeaveBalance> balanceWriter, IReadRepository<BlackoutPeriod> blackouts,
        IReadRepository<Holiday> holidays, IWriteRepository<ApprovalChain> chainWriter, LeaveApprovalChainBuilder chainBuilder,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _employees = employees;
        _leaveTypes = leaveTypes;
        _leavePolicies = leavePolicies;
        _leaveRequests = leaveRequests;
        _leaveRequestWriter = leaveRequestWriter;
        _balances = balances;
        _balanceWriter = balanceWriter;
        _blackouts = blackouts;
        _holidays = holidays;
        _chainWriter = chainWriter;
        _chainBuilder = chainBuilder;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SubmitLeaveRequestResponse>> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(Error.Unauthorized("leave_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(
                Error.Validation("leave_request.no_employee_profile", "This user has no linked employee profile."));
        }

        var employee = await _employees.FirstOrDefaultAsync(new EmployeeByIdSpecification(tenantId, employeeId), cancellationToken);
        if (employee is null)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(Error.NotFound("leave_request.employee_not_found", "Employee not found."));
        }

        var leaveTypeId = new LeaveTypeId(request.LeaveTypeId);
        var leaveType = await _leaveTypes.FirstOrDefaultAsync(new LeaveTypeByIdSpecification(tenantId, leaveTypeId), cancellationToken);
        if (leaveType is null)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(Error.NotFound("leave_request.leave_type_not_found", "Leave type not found."));
        }

        var periodResult = DateRange.Create(request.From, request.To);
        if (periodResult.IsFailure)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(periodResult.Error);
        }

        var period = periodResult.Value;

        var eligibilityError = CheckEligibility(leaveType, employee, today);
        if (eligibilityError is not null)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(eligibilityError);
        }

        var blackouts = await _blackouts.ListAsync(new BlackoutPeriodsByTenantSpecification(tenantId), cancellationToken);
        if (blackouts.Any(b => b.AppliesTo(leaveTypeId) && b.Overlaps(period)))
        {
            return Result.Failure<SubmitLeaveRequestResponse>(
                Error.Conflict("leave_request.blackout_period", "The requested dates fall within a blackout period."));
        }

        var existingRequests = await _leaveRequests.ListAsync(new OverlappingLeaveRequestsSpecification(tenantId, employee.Id), cancellationToken);
        if (existingRequests.Any(r => r.Period.Overlaps(period)))
        {
            return Result.Failure<SubmitLeaveRequestResponse>(
                Error.Conflict("leave_request.overlaps_existing", "This employee already has a pending or approved request overlapping these dates."));
        }

        var policy = await _leavePolicies.FirstOrDefaultAsync(
            new LeavePolicyByLeaveTypeSpecification(tenantId, leaveTypeId, today), cancellationToken);
        if (policy is null)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(
                Error.NotFound("leave_request.no_active_policy", "No active leave policy is configured for this leave type."));
        }

        var holidays = await _holidays.ListAsync(
            new HolidaysByLocationAndRangeSpecification(tenantId, employee.LocationId, period.Start, period.End), cancellationToken);
        var holidayDates = holidays.Select(h => h.Date).ToHashSet();

        var requestedDays = new LeaveDayCounter().CountDays(period, holidayDates, policy.SandwichLeaveEnabled);
        if (requestedDays <= 0)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(
                Error.Validation("leave_request.zero_days", "The requested period contains no working days to charge."));
        }

        var chainSequenceResult = await _chainBuilder.BuildApproverSequenceAsync(tenantId, employee.Id, policy, requestedDays, today, cancellationToken);
        if (chainSequenceResult.IsFailure)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(chainSequenceResult.Error);
        }

        var existingBalance = await _balances.FirstOrDefaultAsync(
            new LeaveBalanceByEmployeeAndTypeSpecification(tenantId, employee.Id, leaveTypeId), cancellationToken);
        var isNewBalance = existingBalance is null;
        var balance = existingBalance ?? LeaveBalance.Open(tenantId, employee.Id, leaveTypeId);

        var balanceOutcome = ResolvePaidAndLopDays(policy, balance, requestedDays, request.AcknowledgeInsufficientBalance);
        if (balanceOutcome.IsFailure)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(balanceOutcome.Error);
        }

        var (paidDays, lopDays) = balanceOutcome.Value;

        var leaveRequestResult = LeaveRequest.Submit(
            tenantId, employee.Id, leaveTypeId, period, requestedDays, request.Reason, now, callerUser.Email.Value);
        if (leaveRequestResult.IsFailure)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(leaveRequestResult.Error);
        }

        var leaveRequest = leaveRequestResult.Value;
        if (lopDays > 0)
        {
            leaveRequest.FlagLossOfPay(lopDays);
        }

        if (paidDays > 0)
        {
            var minimumAllowedBalance = policy.NegativeBalancePolicy == NegativeBalancePolicy.AllowNegative
                ? -policy.MaxNegativeBalanceDays
                : (decimal?)null;

            var postResult = balance.PostEntry(
                LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, paidDays, "Leave request submitted", now, callerUser.Email.Value,
                sourceType: "LeaveRequest", sourceId: leaveRequest.Id.Value, minimumAllowedBalance: minimumAllowedBalance);
            if (postResult.IsFailure)
            {
                return Result.Failure<SubmitLeaveRequestResponse>(postResult.Error);
            }
        }

        var chainResult = ApprovalChain.Create(tenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value, chainSequenceResult.Value, now);
        if (chainResult.IsFailure)
        {
            return Result.Failure<SubmitLeaveRequestResponse>(chainResult.Error);
        }

        await _leaveRequestWriter.AddAsync(leaveRequest, cancellationToken);
        if (isNewBalance)
        {
            await _balanceWriter.AddAsync(balance, cancellationToken);
        }
        else
        {
            _balanceWriter.Update(balance);
        }

        await _chainWriter.AddAsync(chainResult.Value, cancellationToken);

        return Result.Success(new SubmitLeaveRequestResponse(leaveRequest.Id.Value, requestedDays, lopDays));
    }

    private static Error? CheckEligibility(LeaveType leaveType, Employee employee, DateOnly asOf)
    {
        if (leaveType.ApplicableGender is { } requiredGender && employee.Gender != requiredGender)
        {
            return Error.Validation("leave_request.gender_not_eligible", "This leave type is not applicable to this employee.");
        }

        if (leaveType.MinimumTenureMonths > 0)
        {
            var tenureMonths = ((asOf.Year - employee.DateOfJoining.Year) * 12) + asOf.Month - employee.DateOfJoining.Month
                - (asOf.Day < employee.DateOfJoining.Day ? 1 : 0);
            if (tenureMonths < leaveType.MinimumTenureMonths)
            {
                return Error.Validation("leave_request.tenure_not_eligible", "This employee has not yet met the minimum tenure for this leave type.");
            }
        }

        return null;
    }

    private static Result<(decimal PaidDays, decimal LopDays)> ResolvePaidAndLopDays(
        LeavePolicy policy, LeaveBalance balance, decimal requestedDays, bool acknowledgeInsufficientBalance)
    {
        switch (policy.NegativeBalancePolicy)
        {
            case NegativeBalancePolicy.AllowNegative:
                return Result.Success<(decimal, decimal)>((requestedDays, 0m));

            case NegativeBalancePolicy.NotAllowed:
                if (requestedDays > Math.Max(0, balance.Available))
                {
                    return Result.Failure<(decimal, decimal)>(Error.Validation(
                        "leave_request.insufficient_balance", "Insufficient leave balance; this policy does not allow loss-of-pay."));
                }

                return Result.Success<(decimal, decimal)>((requestedDays, 0m));

            default: // AllowWithLop
                var lopDays = Math.Min(requestedDays, new LossOfPayCalculator().CalculateLopDays(balance, requestedDays));
                if (lopDays > 0 && !acknowledgeInsufficientBalance)
                {
                    return Result.Failure<(decimal, decimal)>(Error.Validation(
                        "leave_request.insufficient_balance_requires_ack",
                        $"This request would route {lopDays} day(s) to loss-of-pay. Resubmit with acknowledgement to proceed."));
                }

                return Result.Success<(decimal, decimal)>((requestedDays - lopDays, lopDays));
        }
    }
}
