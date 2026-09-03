using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateLeavePolicyCommandHandlerTests
{
    private readonly IWriteRepository<LeavePolicy> _writer = Substitute.For<IWriteRepository<LeavePolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private CreateLeavePolicyCommandHandler CreateHandler() => new(_writer, _tenantContext);

    [Fact]
    public async Task Handle_Should_Create_A_LeavePolicy_And_Return_Its_Id()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var command = new CreateLeavePolicyCommand(Guid.NewGuid(), 12, 1, 5, new DateOnly(2026, 1, 1));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _writer.Received(1).AddAsync(Arg.Any<LeavePolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_AnnualEntitlementDays_Is_Negative()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var command = new CreateLeavePolicyCommand(Guid.NewGuid(), -1, 1, 5, new DateOnly(2026, 1, 1));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_entitlement");
        await _writer.DidNotReceive().AddAsync(Arg.Any<LeavePolicy>(), Arg.Any<CancellationToken>());
    }
}
