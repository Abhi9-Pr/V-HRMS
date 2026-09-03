using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CreateCandidateCommandHandlerTests
{
    private readonly IWriteRepository<Candidate> _candidates = Substitute.For<IWriteRepository<Candidate>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private CreateCandidateCommandHandler CreateHandler() => new(_candidates, _tenantContext);

    [Fact]
    public async Task Handle_Should_Create_And_Persist_A_Candidate()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var result = await CreateHandler().Handle(
            new CreateCandidateCommand(Guid.NewGuid(), "Jordan Lee", "jordan.lee@example.com", "+14155552671", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _candidates.Received(1).AddAsync(Arg.Is<Candidate>(c => c.FullName == "Jordan Lee"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_For_An_Invalid_Email()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var result = await CreateHandler().Handle(
            new CreateCandidateCommand(Guid.NewGuid(), "Jordan Lee", "not-an-email", "+14155552671", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _candidates.DidNotReceive().AddAsync(Arg.Any<Candidate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_For_An_Invalid_Phone()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var result = await CreateHandler().Handle(
            new CreateCandidateCommand(Guid.NewGuid(), "Jordan Lee", "jordan.lee@example.com", "bad-phone", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _candidates.DidNotReceive().AddAsync(Arg.Any<Candidate>(), Arg.Any<CancellationToken>());
    }
}
