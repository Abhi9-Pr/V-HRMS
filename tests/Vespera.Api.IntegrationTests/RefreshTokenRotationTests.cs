using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

public class RefreshTokenRotationTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public RefreshTokenRotationTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Refresh_Should_Rotate_The_Token_And_Return_A_Working_New_One()
    {
        var (_, login) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-rotate");
        var client = await _factory.CreateTenantScopedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new { refreshToken = login.RefreshToken, deviceId = "device-priya-rotate" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await response.Content.ReadFromJsonAsync<LoginResponse>();
        rotated!.RefreshToken.Should().NotBe(login.RefreshToken);
        rotated.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_Should_Fail_When_The_Same_Token_Is_Presented_A_Second_Time()
    {
        var (_, login) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-reuse");
        var client = await _factory.CreateTenantScopedClientAsync();
        var refreshBody = new { refreshToken = login.RefreshToken, deviceId = "device-priya-reuse" };

        var first = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshBody);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshBody);

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Reuse_Should_Revoke_The_Whole_Family_Not_Just_The_Replayed_Token()
    {
        var (_, login) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-family");
        var client = await _factory.CreateTenantScopedClientAsync();
        var originalBody = new { refreshToken = login.RefreshToken, deviceId = "device-priya-family" };

        // Rotate once (produces a valid "child" token), then replay the original (now-revoked)
        // token — reuse detection should kick in and revoke the child too.
        var rotateResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", originalBody);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var replayResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", originalBody);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var childAttempt = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new { refreshToken = rotated!.RefreshToken, deviceId = "device-priya-family" });

        childAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
