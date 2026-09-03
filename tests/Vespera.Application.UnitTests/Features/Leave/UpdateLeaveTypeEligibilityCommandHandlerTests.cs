using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class UpdateLeaveTypeEligibilityCommandHandlerTests
{
    private readonly IReadRepository<LeaveType> _leaveTypes = Substitute.For<IReadRepository<LeaveType>>();
    private readonly IWriteRepository<LeaveType> _writer = Substitute.For<IWriteRepository<LeaveType>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private UpdateLeaveTypeEligibilityCommandHandler CreateHandler() => new(_leaveTypes, _writer, _tenantContext, _currentUser, _dateTimeProvider);

    private LeaveType CreateLeaveType() =>
        LeaveType.Create(_tenantId, "Maternity Leave", true, 0, DateTimeOffset.UtcNow, "system").Value;

    [Fact]
    public async Task Handle_Should_Update_Eligibility_Rules()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var leaveType = CreateLeaveType();
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);

        var command = new UpdateLeaveTypeEligibilityCommand(leaveType.Id.Value, "Female", 6, true, 10);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveType.MinimumTenureMonths.Should().Be(6);
        leaveType.IsEncashable.Should().BeTrue();
        _writer.Received(1).Update(leaveType);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveType_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var result = await CreateHandler().Handle(new UpdateLeaveTypeEligibilityCommand(Guid.NewGuid(), null, 0, false, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_MinimumTenureMonths_Is_Negative()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var leaveType = CreateLeaveType();
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);

        var command = new UpdateLeaveTypeEligibilityCommand(leaveType.Id.Value, null, -1, false, 0);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _writer.DidNotReceive().Update(Arg.Any<LeaveType>());
    }
}
