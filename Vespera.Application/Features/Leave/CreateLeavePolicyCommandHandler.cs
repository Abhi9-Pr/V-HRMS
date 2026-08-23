using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class CreateLeavePolicyCommandHandler : IRequestHandler<CreateLeavePolicyCommand, Result<Guid>>
{
    private readonly IWriteRepository<LeavePolicy> _writer;
    private readonly ITenantContext _tenantContext;

    public CreateLeavePolicyCommandHandler(IWriteRepository<LeavePolicy> writer, ITenantContext tenantContext)
    {
        _writer = writer;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var result = LeavePolicy.Create(
            _tenantContext.TenantId, new LeaveTypeId(request.LeaveTypeId), request.AnnualEntitlementDays, request.AccrualRatePerMonth,
            request.MaxCarryForwardDays, request.ValidFrom, null);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _writer.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
