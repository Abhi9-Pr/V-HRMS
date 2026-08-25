using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees.Documents;

public sealed record GetEmployeeDocumentsQuery(Guid EmployeeId) : IRequest<Result<IReadOnlyList<EmployeeDocumentDto>>>;

public sealed record EmployeeDocumentDto(
    Guid Id,
    string DocumentType,
    string ScanStatus,
    string VerificationStatus,
    string? RejectionReason,
    DateTimeOffset UploadedAt);
