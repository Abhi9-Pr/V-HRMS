using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class ConvertCandidateToEmployeeCommandHandlerTests
{
    private readonly IReadRepository<Candidate> _candidateReads = Substitute.For<IReadRepository<Candidate>>();
    private readonly IWriteRepository<Candidate> _candidates = Substitute.For<IWriteRepository<Candidate>>();
    private readonly IReadRepository<OfferLetter> _offerLetters = Substitute.For<IReadRepository<OfferLetter>>();
    private readonly IReadRepository<JobRequisition> _requisitions = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly IWriteRepository<Employee> _employees = Substitute.For<IWriteRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly DepartmentId _departmentId = DepartmentId.New();
    private readonly DesignationId _designationId = DesignationId.New();

    public ConvertCandidateToEmployeeCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
    }

    private ConvertCandidateToEmployeeCommandHandler CreateHandler() =>
        new(_candidateReads, _candidates, _offerLetters, _requisitions, _employees, _tenantContext, _currentUser, _dateTimeProvider);

    private (Candidate Candidate, OfferLetter Offer, JobRequisition Requisition) CreateAcceptedOffer()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", _departmentId, 1, DateTimeOffset.UtcNow, "system").Value;
        var candidate = Candidate.Create(
            _tenantId, requisition.Id, "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        var offer = OfferLetter.Draft(_tenantId, candidate.Id, _designationId, Money.Of(1_800_000m, Currency.Inr), new DateOnly(2026, 6, 1));
        offer.Send();
        offer.Accept(DateTimeOffset.UtcNow);

        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        _offerLetters.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);
        _requisitions.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);

        return (candidate, offer, requisition);
    }

    [Fact]
    public async Task Handle_Should_Create_An_Employee_With_No_Re_Keying_Of_Ats_Captured_Fields()
    {
        var (candidate, offer, requisition) = CreateAcceptedOffer();

        var result = await CreateHandler().Handle(
            new ConvertCandidateToEmployeeCommand(candidate.Id.Value, offer.Id.Value, "EMP-900", new DateOnly(1996, 7, 22), LocationId.New().Value, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _employees.Received(1).AddAsync(
            Arg.Is<Employee>(e =>
                e.FirstName == "Jordan" && e.LastName == "Lee"
                && e.WorkEmail == candidate.Email && e.Phone == candidate.Phone
                && e.DepartmentId == requisition.DepartmentId && e.DesignationId == offer.ProposedDesignationId
                && e.DateOfJoining == offer.JoiningDate),
            Arg.Any<CancellationToken>());

        candidate.Status.Should().Be(CandidateStatus.Hired);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Offer_Is_Not_Accepted()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", _departmentId, 1, DateTimeOffset.UtcNow, "system").Value;
        var candidate = Candidate.Create(
            _tenantId, requisition.Id, "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        var offer = OfferLetter.Draft(_tenantId, candidate.Id, _designationId, Money.Of(1_800_000m, Currency.Inr), new DateOnly(2026, 6, 1));

        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        _offerLetters.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);

        var result = await CreateHandler().Handle(
            new ConvertCandidateToEmployeeCommand(candidate.Id.Value, offer.Id.Value, "EMP-900", new DateOnly(1996, 7, 22), LocationId.New().Value, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _employees.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Offer_Belongs_To_A_Different_Candidate()
    {
        var (_, offer, _) = CreateAcceptedOffer();
        var otherCandidateId = Guid.NewGuid();

        var result = await CreateHandler().Handle(
            new ConvertCandidateToEmployeeCommand(otherCandidateId, offer.Id.Value, "EMP-900", new DateOnly(1996, 7, 22), LocationId.New().Value, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _employees.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
