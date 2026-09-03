using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetLeaveTypesETagQueryHandlerTests
{
    private readonly IReadRepository<LeaveType> _leaveTypes = Substitute.For<IReadRepository<LeaveType>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetLeaveTypesETagQueryHandler CreateHandler() => new(_leaveTypes, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Deterministic_Hash_For_The_Same_LeaveTypes()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        var leaveType = LeaveType.Create(tenantId, "Earned Leave", true, 5, Now, "system").Value;
        _leaveTypes.ListAsync(Arg.Any<LeaveTypesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveType> { leaveType });

        var firstResult = await CreateHandler().Handle(new GetLeaveTypesETagQuery(), CancellationToken.None);
        var secondResult = await CreateHandler().Handle(new GetLeaveTypesETagQuery(), CancellationToken.None);

        firstResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Should().Be(secondResult.Value);
        firstResult.Value.Should().HaveLength(64);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Different_Hash_When_LeaveTypes_Differ()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        var leaveType = LeaveType.Create(tenantId, "Earned Leave", true, 5, Now, "system").Value;
        _leaveTypes.ListAsync(Arg.Any<LeaveTypesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveType> { leaveType });
        var emptyResult = await CreateHandler().Handle(new GetLeaveTypesETagQuery(), CancellationToken.None);

        _leaveTypes.ListAsync(Arg.Any<LeaveTypesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveType>());
        var otherResult = await CreateHandler().Handle(new GetLeaveTypesETagQuery(), CancellationToken.None);

        emptyResult.Value.Should().NotBe(otherResult.Value);
    }
}
