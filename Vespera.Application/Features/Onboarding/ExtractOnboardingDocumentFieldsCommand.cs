using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record ExtractOnboardingDocumentFieldsCommand(Guid OnboardingDraftId, Guid EmployeeDocumentId) : IRequest<Result<OcrExtractionResultDto>>;

/// <summary>A suggestion only — see the handler and <c>EmployeeDocument.RecordOcrSuggestion</c>.
/// Nothing about receiving this DTO changes any employee/draft data by itself.</summary>
public sealed record OcrExtractionResultDto(string ExtractedText, IReadOnlyDictionary<string, string> Fields, double Confidence);
