using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class AcknowledgeAnnouncementCommandHandler : IRequestHandler<AcknowledgeAnnouncementCommand, Result>
{
    private readonly IReadRepository<AnnouncementReceipt> _receipts;
    private readonly IWriteRepository<AnnouncementReceipt> _receiptWriter;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public AcknowledgeAnnouncementCommandHandler(
        IReadRepository<AnnouncementReceipt> receipts, IWriteRepository<AnnouncementReceipt> receiptWriter, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _receipts = receipts;
        _receiptWriter = receiptWriter;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(AcknowledgeAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure(Error.Validation("announcement_receipt.no_employee", "The signed-in account is not linked to an employee."));
        }

        var tenantId = _tenantContext.TenantId;
        var announcementId = new AnnouncementId(request.AnnouncementId);

        var receipt = await _receipts.FirstOrDefaultAsync(
            new AnnouncementReceiptByAnnouncementAndEmployeeSpecification(tenantId, announcementId, employeeId.Value), cancellationToken);

        if (receipt is null)
        {
            receipt = AnnouncementReceipt.Create(tenantId, announcementId, employeeId.Value);
            var acknowledgeResult = receipt.Acknowledge(_dateTimeProvider.UtcNow);
            if (acknowledgeResult.IsFailure)
            {
                return acknowledgeResult;
            }

            await _receiptWriter.AddAsync(receipt, cancellationToken);
            return Result.Success();
        }

        var result = receipt.Acknowledge(_dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        _receiptWriter.Update(receipt);
        return Result.Success();
    }
}
