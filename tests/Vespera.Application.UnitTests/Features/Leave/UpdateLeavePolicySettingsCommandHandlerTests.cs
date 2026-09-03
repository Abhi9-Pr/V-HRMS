using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class UpdateLeavePolicySettingsCommandHandlerTests
{
    private readonly IReadRepository<LeavePolicy> _policies = Substitute.For<IReadRepository<LeavePolicy>>();
    private readonly IWriteRepository<LeavePolicy> _writer = Substitute.For<IWriteRepository<LeavePolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    private UpdateLeavePolicySettingsCommandHandler CreateHandler() => new(_policies, _writer, _tenantContext);

    private LeavePolicy CreatePolicy() =>
        LeavePolicy.Create(_tenantId, LeaveTypeId.New(), 12, 1, 5, new DateOnly(2026, 1, 1), null).Value;

    private static UpdateLeavePolicySettingsCommand ValidCommand(Guid policyId) => new(
        policyId, "Monthly", 0, false, null, false, "NotAllowed", 0, false);

    [Fact]
    public async Task Handle_Should_Update_All_Three_Configuration_Surfaces()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var policy = CreatePolicy();
        _policies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(policy);

        var command = new UpdateLeavePolicySettingsCommand(
            policy.Id.Value, "Annual", 3, true, 10, true, "AllowWithLop", 5, true);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        policy.RequiresHrApproval.Should().BeTrue();
        policy.SandwichLeaveEnabled.Should().BeTrue();
        _writer.Received(1).Update(policy);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Policy_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _policies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((LeavePolicy?)null);

        var result = await CreateHandler().Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_SkipLevelThresholdDays_Is_Negative()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var policy = CreatePolicy();
        _policies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(policy);

        var command = ValidCommand(policy.Id.Value) with { SkipLevelThresholdDays = -1 };
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_skip_level_threshold");
        _writer.DidNotReceive().Update(Arg.Any<LeavePolicy>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_MaxNegativeBalanceDays_Is_Negative()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var policy = CreatePolicy();
        _policies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(policy);

        var command = ValidCommand(policy.Id.Value) with { MaxNegativeBalanceDays = -1 };
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_max_negative_balance");
    }
}
