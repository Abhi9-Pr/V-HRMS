using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class UploadEmployeeDocumentCommandHandler : IRequestHandler<UploadEmployeeDocumentCommand, Result<Guid>>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IVirusScanner _virusScanner;

    public UploadEmployeeDocumentCommandHandler(
        IReadRepository<Employee> employees,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IVirusScanner virusScanner)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _virusScanner = virusScanner;
    }

    public async Task<Result<Guid>> Handle(UploadEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Guid>(Error.NotFound("employee.not_found", "Employee not found."));
        }

        // Scanned before anything is written to storage — an infected file is never persisted,
        // not even briefly, rather than uploaded-then-deleted.
        var scanResult = await _virusScanner.ScanAsync(new MemoryStream(request.Content), cancellationToken);
        if (scanResult == ScanResult.Infected)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "employee_document.infected", "The uploaded file failed a virus scan and was not stored."));
        }

        var storageKey = await _fileStorage.UploadAsync(request.FileName, new MemoryStream(request.Content), cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        var document = employee.AddDocument(request.DocumentType, storageKey, now);
        document.MarkScanned(scanResult == ScanResult.Clean ? DocumentScanStatus.Clean : DocumentScanStatus.Failed);

        return Result.Success(document.Id.Value);
    }
}
