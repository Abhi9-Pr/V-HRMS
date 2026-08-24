using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Recruitment;

/// <summary>
/// "No re-keying" — name, email, phone, proposed designation, joining date, and department all
/// flow straight from the ATS data already captured on the <see cref="Candidate"/>/
/// <see cref="OfferLetter"/>/<see cref="JobRequisition"/>. Only <see cref="EmployeeCode"/>,
/// date of birth, and work location are required as explicit inputs here, because nothing in the
/// ATS domain model ever captures those. Splitting <see cref="Candidate.FullName"/> on its first
/// space into first/last name is a deliberate simplification — a real intake form would capture
/// them separately.
/// </summary>
public sealed class ConvertCandidateToEmployeeCommandHandler : IRequestHandler<ConvertCandidateToEmployeeCommand, Result<Guid>>
{
    private readonly IReadRepository<Candidate> _candidateReads;
    private readonly IWriteRepository<Candidate> _candidates;
    private readonly IReadRepository<OfferLetter> _offerLetters;
    private readonly IReadRepository<JobRequisition> _requisitions;
    private readonly IWriteRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConvertCandidateToEmployeeCommandHandler(
        IReadRepository<Candidate> candidateReads,
        IWriteRepository<Candidate> candidates,
        IReadRepository<OfferLetter> offerLetters,
        IReadRepository<JobRequisition> requisitions,
        IWriteRepository<Employee> employees,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _candidateReads = candidateReads;
        _candidates = candidates;
        _offerLetters = offerLetters;
        _requisitions = requisitions;
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(ConvertCandidateToEmployeeCommand request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetters.FirstOrDefaultAsync(
            new OfferLetterByIdSpecification(new OfferLetterId(request.OfferLetterId)), cancellationToken);

        if (offer is null)
        {
            return Result.Failure<Guid>(Error.NotFound("offer_letter.not_found", "Offer letter not found."));
        }

        if (offer.CandidateId.Value != request.CandidateId)
        {
            return Result.Failure<Guid>(Error.Conflict("offer_letter.candidate_mismatch", "This offer does not belong to the given candidate."));
        }

        if (offer.Status != OfferLetterStatus.Accepted)
        {
            return Result.Failure<Guid>(Error.Conflict("offer_letter.not_accepted", "Only an accepted offer can be converted into an employee."));
        }

        var candidate = await _candidateReads.FirstOrDefaultAsync(
            new CandidateByIdSpecification(offer.CandidateId), cancellationToken);

        if (candidate is null)
        {
            return Result.Failure<Guid>(Error.NotFound("candidate.not_found", "Candidate not found."));
        }

        var requisition = await _requisitions.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(candidate.JobRequisitionId), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure<Guid>(Error.NotFound("candidate.requisition_not_found", "The candidate's job requisition was not found."));
        }

        var codeResult = EmployeeCode.Create(request.EmployeeCode);
        if (codeResult.IsFailure)
        {
            return Result.Failure<Guid>(codeResult.Error);
        }

        var nameParts = candidate.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : nameParts[0];

        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";

        var employeeResult = Employee.Onboard(
            _tenantContext.TenantId, codeResult.Value, firstName, lastName, candidate.Email, candidate.Phone,
            request.DateOfBirth, offer.JoiningDate, requisition.DepartmentId, offer.ProposedDesignationId, new LocationId(request.LocationId),
            now, createdBy);

        if (employeeResult.IsFailure)
        {
            return Result.Failure<Guid>(employeeResult.Error);
        }

        await _employees.AddAsync(employeeResult.Value, cancellationToken);

        if (candidate.Status != CandidateStatus.Hired)
        {
            candidate.MarkHired();
            _candidates.Update(candidate);
        }

        return Result.Success(employeeResult.Value.Id.Value);
    }
}
