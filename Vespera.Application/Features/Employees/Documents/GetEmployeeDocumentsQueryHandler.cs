using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class GetEmployeeDocumentsQueryHandler : IRequestHandler<GetEmployeeDocumentsQuery, Result<IReadOnlyList<EmployeeDocumentDto>>>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;

    public GetEmployeeDocumentsQueryHandler(IReadRepository<Employee> employees, ITenantContext tenantContext)
    {
        _employees = employees;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<EmployeeDocumentDto>>> Handle(GetEmployeeDocumentsQuery request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<IReadOnlyList<EmployeeDocumentDto>>(Error.NotFound("employee.not_found", "Employee not found."));
        }

        IReadOnlyList<EmployeeDocumentDto> documents = employee.Documents
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => new EmployeeDocumentDto(
                document.Id.Value,
                document.DocumentType.ToString(),
                document.ScanStatus.ToString(),
                document.VerificationStatus.ToString(),
                document.RejectionReason,
                document.UploadedAt))
            .ToList();

        return Result.Success(documents);
    }
}
