using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetLeavePoliciesQueryHandlerTests
{
    private readonly IReadRepository<LeavePolicy> _policies = Substitute.For<IReadRepository<LeavePolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private GetLeavePoliciesQueryHandler CreateHandler() => new(_policies, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Policies_For_The_Current_Tenant()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        var policy = LeavePolicy.Create(tenantId, LeaveTypeId.New(), 12, 1, 5, new DateOnly(2026, 1, 1), null).Value;
        _policies.ListAsync(Arg.Any<LeavePoliciesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeavePolicy> { policy });

        var result = await CreateHandler().Handle(new GetLeavePoliciesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == policy.Id.Value && d.AnnualEntitlementDays == 12);
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_There_Are_No_Policies()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _policies.ListAsync(Arg.Any<LeavePoliciesByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<LeavePolicy>());

        var result = await CreateHandler().Handle(new GetLeavePoliciesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
