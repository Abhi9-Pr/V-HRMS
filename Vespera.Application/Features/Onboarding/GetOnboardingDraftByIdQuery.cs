using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record GetOnboardingDraftByIdQuery(Guid Id) : IRequest<Result<OnboardingDraftDto>>;

public sealed record OnboardingDraftDto(
    Guid Id,
    string Status,
    string CurrentStep,
    string? FirstName,
    string? LastName,
    string? WorkEmail,
    string? Phone,
    DateOnly? DateOfBirth,
    Guid? DepartmentId,
    Guid? DesignationId,
    Guid? LocationId,
    DateOnly? DateOfJoining,
    Guid? ConvertedEmployeeId,
    IReadOnlyList<OnboardingDraftDocumentDto> Documents,
    IReadOnlyList<OnboardingDraftConsentDto> ConsentRecords);

public sealed record OnboardingDraftDocumentDto(
    Guid Id,
    string DocumentType,
    string ScanStatus,
    bool IsOcrConfirmed,
    double? OcrConfidence);

public sealed record OnboardingDraftConsentDto(string ConsentType, bool Granted, DateTimeOffset GrantedAt);
