using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class DeleteEmployeeDocumentCommandHandler : IRequestHandler<DeleteEmployeeDocumentCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorage _fileStorage;

    public DeleteEmployeeDocumentCommandHandler(IReadRepository<Employee> employees, ITenantContext tenantContext, IFileStorage fileStorage)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _fileStorage = fileStorage;
    }

    public async Task<Result> Handle(DeleteEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var documentId = new EmployeeDocumentId(request.DocumentId);
        var document = employee.Documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
        {
            return Result.Failure(Error.NotFound("employee_document.not_found", "Document not found."));
        }

        var fileReference = document.FileReference;
        var removeResult = employee.RemoveDocument(documentId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        await _fileStorage.DeleteAsync(fileReference, cancellationToken);
        return Result.Success();
    }
}
