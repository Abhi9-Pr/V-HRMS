using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees.Documents;

public sealed record GetEmployeeDocumentDownloadUrlQuery(Guid EmployeeId, Guid DocumentId) : IRequest<Result<DocumentDownloadUrlDto>>;

public sealed record DocumentDownloadUrlDto(string Url, DateTimeOffset ExpiresAt);
