using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily poll that posts each employee's accrual for the current calendar month (or year, for an
/// annual-frequency policy) exactly once, and — every January — forfeits any prior-year balance
/// above its policy's carry-forward cap. Idempotent purely via <see cref="LeaveLedgerEntry.PeriodKey"/>:
/// re-running the same day, or catching up after downtime, never double-posts, so this can simply
/// poll rather than need exact once-a-month scheduling. Same cross-tenant
/// <see cref="IReadRepositoryAdmin{T}"/> + explicit <c>SaveChangesAsync</c> shape as
/// <see cref="AttendanceDayComputationHostedService"/> — there's no ambient tenant context in a
/// hosted service's own DI scope, and hosted services sit outside the MediatR
/// <c>TransactionBehavior</c> that ordinarily commits a command handler's work.
/// </summary>
public sealed class LeaveAccrualHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogSweepCompleted = LoggerMessage.Define<int>(
        LogLevel.Information, new EventId(1, nameof(LogSweepCompleted)), "Leave accrual sweep posted {PostedCount} ledger entries");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<LeaveAccrualHostedService> _logger;

    public LeaveAccrualHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<LeaveAccrualHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_options.Value.LeaveAccrual.RunIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>Runs one sweep. Public so tests can drive it directly.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var policiesAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<LeavePolicy>>();
        var employeesAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Employee>>();
        var balancesAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<LeaveBalance>>();
        var balanceWriter = scope.ServiceProvider.GetRequiredService<IWriteRepository<LeaveBalance>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var calculator = new LeaveAccrualCalculator();

        var activePolicies = await policiesAdmin.ListIgnoringFiltersAsync(new ActiveLeavePoliciesSpecification(today), cancellationToken);
        var activeEmployees = await employeesAdmin.ListIgnoringFiltersAsync(new ActiveEmployeesSpecification(), cancellationToken);

        var postedCount = 0;
        foreach (var policy in activePolicies)
        {
            foreach (var employee in activeEmployees.Where(e => e.TenantId == policy.TenantId))
            {
                var balance = await LoadOrOpenBalanceAsync(balancesAdmin, balanceWriter, policy.TenantId, employee.Id, policy.LeaveTypeId, cancellationToken);

                // Assess the carry-forward cap against the prior year's closing balance before this
                // year's accrual is posted, so the January accrual never inflates what gets forfeited.
                if (today.Month == 1 && PostCarryForwardRollover(balance, policy, today, now))
                {
                    postedCount++;
                }

                if (PostAccrual(balance, policy, employee, calculator, today, now))
                {
                    postedCount++;
                }

                balanceWriter.Update(balance);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSweepCompleted(_logger, postedCount, null);
    }

    private static bool PostAccrual(
        LeaveBalance balance, LeavePolicy policy, Employee employee, LeaveAccrualCalculator calculator, DateOnly today, DateTimeOffset now)
    {
        string periodKey;
        decimal amount;

        if (policy.AccrualFrequency == AccrualFrequency.Annual)
        {
            periodKey = $"{today.Year:D4}-ANNUAL";
            amount = calculator.CalculateAnnualAccrual(policy, employee, today.Year);
        }
        else
        {
            periodKey = today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            amount = calculator.CalculateMonthlyAccrual(policy, employee, monthStart, monthEnd);
        }

        if (amount <= 0 || balance.Entries.Any(e => e.Type == LeaveLedgerEntryType.Accrual && e.PeriodKey == periodKey))
        {
            return false;
        }

        var result = balance.PostEntry(
            LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, amount, "Scheduled accrual", now, "system",
            sourceType: "LeavePolicy", sourceId: policy.Id.Value, periodKey: periodKey);
        return result.IsSuccess;
    }

    private static bool PostCarryForwardRollover(LeaveBalance balance, LeavePolicy policy, DateOnly today, DateTimeOffset now)
    {
        var periodKey = $"{today.Year:D4}-ROLLOVER";
        if (balance.Entries.Any(e => e.Type == LeaveLedgerEntryType.Adjustment && e.PeriodKey == periodKey))
        {
            return false;
        }

        var excess = balance.Available - policy.MaxCarryForwardDays;
        if (excess <= 0)
        {
            return false;
        }

        var result = balance.PostEntry(
            LeaveLedgerEntryType.Adjustment, LeaveLedgerDirection.Debit, excess, "Carry-forward cap forfeiture", now, "system",
            sourceType: "LeavePolicy", sourceId: policy.Id.Value, periodKey: periodKey);
        return result.IsSuccess;
    }

    private static async Task<LeaveBalance> LoadOrOpenBalanceAsync(
        IReadRepositoryAdmin<LeaveBalance> balancesAdmin, IWriteRepository<LeaveBalance> balanceWriter,
        Domain.Common.TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId, CancellationToken cancellationToken)
    {
        var matches = await balancesAdmin.ListIgnoringFiltersAsync(
            new LeaveBalanceByEmployeeAndTypeSpecification(tenantId, employeeId, leaveTypeId), cancellationToken);
        if (matches.Count > 0)
        {
            return matches[0];
        }

        var balance = LeaveBalance.Open(tenantId, employeeId, leaveTypeId);
        await balanceWriter.AddAsync(balance, cancellationToken);
        return balance;
    }

    private sealed class ActiveEmployeesSpecification : ISpecification<Employee>
    {
        public Expression<Func<Employee, bool>> Criteria => e => e.Status == EmploymentStatus.Active && !e.IsDeleted;

        public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

        public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

        public (int Skip, int Take)? Paging => null;
    }
}
