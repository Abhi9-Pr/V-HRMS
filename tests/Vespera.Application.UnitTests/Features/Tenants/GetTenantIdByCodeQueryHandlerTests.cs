using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Tenants;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Tenants;

public class GetTenantIdByCodeQueryHandlerTests
{
    private readonly IReadRepository<Tenant> _tenants = Substitute.For<IReadRepository<Tenant>>();

    private GetTenantIdByCodeQueryHandler CreateHandler() => new(_tenants);

    [Fact]
    public async Task Handle_Should_Return_Tenant_Lookup_When_Code_Matches()
    {
        var tenant = Tenant.Create("Demo Company", "DEMO", DateTimeOffset.UtcNow, "seed").Value;
        _tenants.FirstOrDefaultAsync(Arg.Any<ISpecification<Tenant>>(), Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetTenantIdByCodeQuery("DEMO"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenant.Id.Value);
        result.Value.Name.Should().Be("Demo Company");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Code_Does_Not_Match()
    {
        _tenants.FirstOrDefaultAsync(Arg.Any<ISpecification<Tenant>>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetTenantIdByCodeQuery("NOPE"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("tenant.not_found");
    }
}
