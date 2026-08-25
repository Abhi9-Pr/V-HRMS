using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

public class TenantLookupTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public TenantLookupTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ByCode_Should_Resolve_The_Demo_Tenant_Anonymously()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/tenants/by-code/{DevelopmentSeeder.DemoTenantCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TenantLookupResponse>();
        body!.TenantId.Should().Be(await _factory.GetDemoTenantIdAsync());
    }

    [Fact]
    public async Task ByCode_Should_Return_NotFound_For_An_Unknown_Code()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/tenants/by-code/NOPE");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record TenantLookupResponse(Guid TenantId, string Name);
}
