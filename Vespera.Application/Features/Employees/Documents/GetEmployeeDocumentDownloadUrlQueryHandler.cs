using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class GetEmployeeDocumentDownloadUrlQueryHandler : IRequestHandler<GetEmployeeDocumentDownloadUrlQuery, Result<DocumentDownloadUrlDto>>
{
    private static readonly TimeSpan LinkExpiry = TimeSpan.FromMinutes(15);

    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;

    public GetEmployeeDocumentDownloadUrlQueryHandler(
        IReadRepository<Employee> employees,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
    }

    public async Task<Result<DocumentDownloadUrlDto>> Handle(GetEmployeeDocumentDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<DocumentDownloadUrlDto>(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var document = employee.Documents.FirstOrDefault(d => d.Id == new EmployeeDocumentId(request.DocumentId));
        if (document is null)
        {
            return Result.Failure<DocumentDownloadUrlDto>(Error.NotFound("employee_document.not_found", "Document not found."));
        }

        if (document.ScanStatus != DocumentScanStatus.Clean)
        {
            return Result.Failure<DocumentDownloadUrlDto>(Error.Conflict(
                "employee_document.not_clean", "This document has not cleared its virus scan and cannot be downloaded."));
        }

        var url = await _fileStorage.GetDownloadUrlAsync(document.FileReference, LinkExpiry, cancellationToken);
        var expiresAt = _dateTimeProvider.UtcNow + LinkExpiry;

        return Result.Success(new DocumentDownloadUrlDto(url.ToString(), expiresAt));
    }
}
