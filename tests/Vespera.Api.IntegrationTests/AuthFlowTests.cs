using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

public class AuthFlowTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public AuthFlowTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Should_Succeed_For_A_Seeded_Non_Finance_User()
    {
        var client = await _factory.CreateTenantScopedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "priya.sharma@demo.vespera.test",
            password = DevelopmentSeeder.DemoPassword,
            deviceId = "device-priya-1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        login!.AccessToken.Should().NotBeNullOrEmpty();
        login.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Should_Fail_With_Wrong_Password()
    {
        var client = await _factory.CreateTenantScopedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "priya.sharma@demo.vespera.test",
            password = "wrong-password-entirely",
            deviceId = "device-priya-1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Should_Require_Totp_For_A_Finance_Admin_User()
    {
        var client = await _factory.CreateTenantScopedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "vikram.nair@demo.vespera.test",
            password = DevelopmentSeeder.DemoPassword,
            deviceId = "device-vikram-1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Should_Succeed_For_A_Finance_Admin_With_A_Valid_Totp_Code()
    {
        var client = await _factory.CreateTenantScopedClientAsync();
        var code = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "vikram.nair@demo.vespera.test",
            password = DevelopmentSeeder.DemoPassword,
            deviceId = "device-vikram-1",
            totpCode = code,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Should_Revoke_The_Presented_Refresh_Token()
    {
        var (client, login) = await _factory.CreateAuthenticatedClientAsync(
            "ananya.iyer@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-ananya-logout");

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = login.RefreshToken });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tenantClient = await _factory.CreateTenantScopedClientAsync();
        var refreshResponse = await tenantClient.PostAsJsonAsync(
            "/api/v1/auth/refresh", new { refreshToken = login.RefreshToken, deviceId = "device-ananya-logout" });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Should_Fail_With_The_Wrong_Current_Password()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "ananya.iyer@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-ananya-changepw");

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password", new { currentPassword = "not-the-real-password", newPassword = "SomethingNew!2345" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_Should_Always_Return_NoContent_Even_For_An_Unknown_Email()
    {
        var client = await _factory.CreateTenantScopedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "nobody-like-this@demo.vespera.test" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
