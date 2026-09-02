using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the Employees CRUD slice (M2) works end to end against the real pipeline —
/// copied from DepartmentsCrudTests.cs's shape.</summary>
public class EmployeesCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public EmployeesCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Get_Update_Transfer_Exit_Should_Round_Trip_For_A_User_With_Employees_Write()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-employees-crud");

        var departmentResponse = await client.PostAsJsonAsync("/api/v1/departments", new { name = "R&D", code = $"RND-{Guid.NewGuid():N}"[..16] });
        var departmentBody = await departmentResponse.Content.ReadAsStringAsync();
        departmentResponse.StatusCode.Should().Be(HttpStatusCode.OK, departmentBody);
        var department = JsonSerializer.Deserialize<CreatedResponse>(departmentBody, JsonOptions);
        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Researcher-{Guid.NewGuid():N}", grade = 3 });
        var designationBody = await designationResponse.Content.ReadAsStringAsync();
        designationResponse.StatusCode.Should().Be(HttpStatusCode.OK, designationBody);
        var designation = JsonSerializer.Deserialize<CreatedResponse>(designationBody, JsonOptions);
        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "R&D Campus", addressLine = "2 Innovation Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        locationResponse.EnsureSuccessStatusCode();
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            code = $"EMP-{Guid.NewGuid():N}"[..12],
            firstName = "Marie",
            lastName = "Curie",
            workEmail = $"marie.curie.{Guid.NewGuid():N}@vespera.test",
            phone = "+33612345678",
            dateOfBirth = "1990-01-01",
            dateOfJoining = "2026-01-15",
            departmentId = department!.Id,
            designationId = designation!.Id,
            locationId = location!.Id,
        });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK, createBody);
        var created = JsonSerializer.Deserialize<CreatedResponse>(createBody, JsonOptions);
        var id = created!.Id;

        var getResponse = await client.GetAsync($"/api/v1/employees/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        fetched!.FirstName.Should().Be("Marie");
        fetched.MaskedPan.Should().BeNull();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/employees/{id}", new
        {
            firstName = "Marie",
            lastName = "Sklodowska-Curie",
            workEmail = fetched.WorkEmail,
            phone = fetched.Phone,
            pan = "ABCDE1234F",
            bankAccount = (string?)null,
            annualCtcAmount = 1500000m,
            annualCtcCurrency = "Inr",
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterUpdateResponse = await client.GetAsync($"/api/v1/employees/{id}");
        var afterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        afterUpdate!.LastName.Should().Be("Sklodowska-Curie");
        afterUpdate.MaskedPan.Should().NotBeNull();
        afterUpdate.MaskedPan.Should().NotBe("ABCDE1234F");
        afterUpdate.HasCompensationOnRecord.Should().BeTrue();

        var transferResponse = await client.PostAsJsonAsync($"/api/v1/employees/{id}/transfer", new
        {
            departmentId = department.Id,
            designationId = designation.Id,
            locationId = location.Id,
            effectiveDate = "2026-06-01",
            reason = "Promotion",
        });
        transferResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/v1/employees?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var exitResponse = await client.PostAsJsonAsync($"/api/v1/employees/{id}/exit", new { exitDate = "2026-12-31", reason = "Resignation" });
        exitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterExitResponse = await client.GetAsync($"/api/v1/employees/{id}");
        var afterExit = await getAfterExitResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        afterExit!.Status.Should().Be("Exited");
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_Employees_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-employees-forbidden");

        var response = await client.GetAsync("/api/v1/employees?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Should_Allow_An_Employee_To_View_Their_Own_Profile_Without_Employees_Read()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-find-priya");
        var priyaId = await FindEmployeeIdByFullNameAsync(hrClient, "Priya Sharma");

        var (priyaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-employees-self");

        var response = await priyaClient.GetAsync($"/api/v1/employees/{priyaId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_Should_Return_Forbidden_For_An_Unrelated_Employee_Without_ReadAny()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-find-ananya");
        var ananyaId = await FindEmployeeIdByFullNameAsync(hrClient, "Ananya Iyer");

        var (priyaClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-employees-unrelated");

        var response = await priyaClient.GetAsync($"/api/v1/employees/{ananyaId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Update_Should_Return_NotFound_For_An_Employee_Owned_By_Another_Tenant()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-employees-idor-owner");

        var departmentResponse = await client.PostAsJsonAsync("/api/v1/departments", new { name = "IDOR Dept", code = $"IDR-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Researcher-{Guid.NewGuid():N}", grade = 3 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new { name = "IDOR Campus", addressLine = "2 Innovation Way", city = "Pune", country = "India", latitude = 18.5204, longitude = 73.8567, timeZoneId = "Asia/Kolkata" });
        var location = await locationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var createResponse = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            code = $"EMP-{Guid.NewGuid():N}"[..12],
            firstName = "Cross",
            lastName = "Tenant",
            workEmail = $"cross.tenant.{Guid.NewGuid():N}@vespera.test",
            phone = "+33612345678",
            dateOfBirth = "1990-01-01",
            dateOfJoining = "2026-01-15",
            departmentId = department!.Id,
            designationId = designation!.Id,
            locationId = location!.Id,
        });
        var id = (await createResponse.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        (await otherTenantClient.GetAsync($"/api/v1/employees/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.PutAsJsonAsync($"/api/v1/employees/{id}", new
        {
            firstName = "Hijacked",
            lastName = "Employee",
            workEmail = $"hijacked.{Guid.NewGuid():N}@vespera.test",
            phone = "+14155552671",
            pan = (string?)null,
            bankAccount = (string?)null,
            annualCtcAmount = (decimal?)null,
            annualCtcCurrency = (string?)null,
        })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> FindEmployeeIdByFullNameAsync(HttpClient client, string fullName)
    {
        var response = await client.GetAsync("/api/v1/employees?page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<EmployeeSummaryPage>();
        return page!.Items.Single(item => item.FullName == fullName).Id;
    }

    [Fact]
    public async Task RevealPiiField_Should_Return_Forbidden_For_A_User_Without_Unmask()
    {
        var (hrClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-employees-unmask-forbidden");

        var response = await hrClient.GetAsync("/api/v1/employees/00000000-0000-0000-0000-000000000000/pii/Pan");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record EmployeeResponse(
        Guid Id, string FirstName, string LastName, string WorkEmail, string Phone, string Status,
        string? MaskedPan, string? MaskedBankAccount, bool HasCompensationOnRecord);

    private sealed record EmployeeSummaryPage(IReadOnlyList<EmployeeSummaryItem> Items);

    private sealed record EmployeeSummaryItem(Guid Id, string Code, string FullName, string Status);
}
