using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed record UploadOnboardingDocumentCommand(
    Guid OnboardingDraftId,
    EmployeeDocumentType DocumentType,
    string FileName,
    byte[] Content) : IRequest<Result<Guid>>;
