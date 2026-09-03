using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class CreateSlaPolicyCommandHandlerTests
{
    private readonly IWriteRepository<SlaPolicy> _policies = Substitute.For<IWriteRepository<SlaPolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateSlaPolicyCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateSlaPolicyCommandHandler CreateHandler() => new(_policies, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Stage_A_New_Sla_Policy_And_Return_Its_Id()
    {
        var command = new CreateSlaPolicyCommand("Standard", 2, 10, new TimeOnly(9, 0), new TimeOnly(18, 0), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _policies.Received(1).AddAsync(Arg.Is<SlaPolicy>(p => p.Name == "Standard"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Name_Is_Blank()
    {
        var command = new CreateSlaPolicyCommand("   ", 2, 10, new TimeOnly(9, 0), new TimeOnly(18, 0), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("sla_policy.name_required");
        await _policies.DidNotReceive().AddAsync(Arg.Any<SlaPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Resolution_Time_Is_Shorter_Than_Response_Time()
    {
        var command = new CreateSlaPolicyCommand("Standard", 10, 2, new TimeOnly(9, 0), new TimeOnly(18, 0), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("sla_policy.invalid_targets");
    }
}
