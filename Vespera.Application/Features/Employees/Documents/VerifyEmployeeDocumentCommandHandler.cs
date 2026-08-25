using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class VerifyEmployeeDocumentCommandHandler : IRequestHandler<VerifyEmployeeDocumentCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;

    public VerifyEmployeeDocumentCommandHandler(IReadRepository<Employee> employees, ITenantContext tenantContext)
    {
        _employees = employees;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(VerifyEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var document = employee.Documents.FirstOrDefault(d => d.Id == new EmployeeDocumentId(request.DocumentId));
        if (document is null)
        {
            return Result.Failure(Error.NotFound("employee_document.not_found", "Document not found."));
        }

        return document.Verify();
    }
}
