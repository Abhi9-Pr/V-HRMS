using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees.Documents;

public sealed record UploadEmployeeDocumentCommand(
    Guid EmployeeId,
    EmployeeDocumentType DocumentType,
    string FileName,
    byte[] Content) : IRequest<Result<Guid>>;
