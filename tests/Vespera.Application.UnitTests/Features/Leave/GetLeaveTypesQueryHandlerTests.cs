using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetLeaveTypesQueryHandlerTests
{
    private readonly IReadRepository<LeaveType> _leaveTypes = Substitute.For<IReadRepository<LeaveType>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetLeaveTypesQueryHandler CreateHandler() => new(_leaveTypes, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Leave_Types_For_The_Current_Tenant()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        var leaveType = LeaveType.Create(tenantId, "Earned Leave", true, 5, Now, "system").Value;
        _leaveTypes.ListAsync(Arg.Any<LeaveTypesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveType> { leaveType });

        var result = await CreateHandler().Handle(new GetLeaveTypesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == leaveType.Id.Value && d.Name == "Earned Leave");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_There_Are_No_Leave_Types()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _leaveTypes.ListAsync(Arg.Any<LeaveTypesByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<LeaveType>());

        var result = await CreateHandler().Handle(new GetLeaveTypesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
