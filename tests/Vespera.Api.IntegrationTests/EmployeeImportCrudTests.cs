using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the bulk employee import slice (M7) works end to end: dry-run reports without
/// persisting, a real run commits every row when all are valid, and a real run with one bad row
/// commits nothing (true all-or-nothing, not partial).</summary>
public class EmployeeImportCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public EmployeeImportCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DryRun_Then_Real_Run_Should_Report_And_Then_Create_Employees()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-import");

        var (departmentCode, designationTitle, locationName) = await CreateMastersAsync(client);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var csv = BuildCsv(
        [
            ($"EMP-IMP-{suffix}-1", "Ada", "Lovelace", $"ada.{suffix}@vespera.test", "+14155550001"),
            ($"EMP-IMP-{suffix}-2", "Alan", "Turing", $"alan.{suffix}@vespera.test", "+14155550002"),
        ], departmentCode, designationTitle, locationName);

        var dryRunResponse = await PostImportAsync(client, csv, dryRun: true);
        dryRunResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dryRunReport = await dryRunResponse.Content.ReadFromJsonAsync<ImportReportResponse>();
        dryRunReport!.Committed.Should().BeFalse();
        dryRunReport.SuccessCount.Should().Be(2);

        // If the dry run had (incorrectly) persisted anything, this real run with the identical
        // codes would fail with "already in use" duplicate-code errors instead of succeeding.
        var realRunResponse = await PostImportAsync(client, csv, dryRun: false);
        realRunResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var realRunReport = await realRunResponse.Content.ReadFromJsonAsync<ImportReportResponse>();
        realRunReport!.Committed.Should().BeTrue();
        realRunReport.SuccessCount.Should().Be(2);

        // And a second real run of the same codes now correctly fails as duplicates, proving the
        // first real run really did commit.
        var repeatResponse = await PostImportAsync(client, csv, dryRun: false);
        var repeatReport = await repeatResponse.Content.ReadFromJsonAsync<ImportReportResponse>();
        repeatReport!.Committed.Should().BeFalse();
        repeatReport.FailureCount.Should().Be(2);
    }

    [Fact]
    public async Task Real_Run_With_One_Invalid_Row_Should_Create_Nothing()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-import-atomic");

        var (departmentCode, designationTitle, locationName) = await CreateMastersAsync(client);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var csv = BuildCsv(
        [
            ($"EMP-IMP-{suffix}-1", "Grace", "Hopper", $"grace.{suffix}@vespera.test", "+14155550003"),
        ], departmentCode, designationTitle, locationName)
            + $"EMP-IMP-{suffix}-2,,Bad,bad-{suffix}@vespera.test,+14155550004,1990-01-01,2026-01-15,{departmentCode},{designationTitle},{locationName}\n";

        var response = await PostImportAsync(client, csv, dryRun: false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<ImportReportResponse>();
        report!.Committed.Should().BeFalse();
        report.SuccessCount.Should().Be(1);
        report.FailureCount.Should().Be(1);

        // Prove the one otherwise-valid row was NOT created despite the batch reporting it as a
        // success: re-importing just that row's code on its own must still succeed (a code that
        // had actually been persisted by the failed batch would instead be rejected as a
        // duplicate here).
        var retryCsv = BuildCsv(
            [($"EMP-IMP-{suffix}-1", "Grace", "Hopper", $"grace.{suffix}@vespera.test", "+14155550003")],
            departmentCode, designationTitle, locationName);
        var retryResponse = await PostImportAsync(client, retryCsv, dryRun: false);
        var retryReport = await retryResponse.Content.ReadFromJsonAsync<ImportReportResponse>();
        retryReport!.Committed.Should().BeTrue();
        retryReport.SuccessCount.Should().Be(1);
    }

    private static async Task<HttpResponseMessage> PostImportAsync(HttpClient client, string csv, bool dryRun)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "employees.csv");
        return await client.PostAsync($"/api/v1/employees/import?dryRun={dryRun}", content);
    }

    private static string BuildCsv(
        IEnumerable<(string Code, string FirstName, string LastName, string Email, string Phone)> rows,
        string departmentCode, string designationTitle, string locationName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Code,FirstName,LastName,WorkEmail,Phone,DateOfBirth,DateOfJoining,DepartmentCode,DesignationTitle,LocationName");
        foreach (var row in rows)
        {
            sb.AppendLine($"{row.Code},{row.FirstName},{row.LastName},{row.Email},{row.Phone},1990-01-01,2026-01-15,{departmentCode},{designationTitle},{locationName}");
        }

        return sb.ToString();
    }

    private static async Task<(string DepartmentCode, string DesignationTitle, string LocationName)> CreateMastersAsync(HttpClient client)
    {
        var departmentCode = $"IMP-{Guid.NewGuid():N}"[..12];
        await client.PostAsJsonAsync("/api/v1/departments", new { name = "Import Dept", code = departmentCode });

        var designationTitle = $"Import Role {Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/designations", new { title = designationTitle, grade = 2 });

        var locationName = $"Import Site {Guid.NewGuid():N}";
        await client.PostAsJsonAsync(
            "/api/v1/locations",
            new
            {
                name = locationName,
                addressLine = "1 Import Way",
                city = "Pune",
                country = "India",
                latitude = 18.5204,
                longitude = 73.8567,
                timeZoneId = "Asia/Kolkata",
            });

        return (departmentCode, designationTitle, locationName);
    }

    private sealed record ImportReportResponse(List<ImportRowResponse> Rows, int SuccessCount, int FailureCount, bool Committed);

    private sealed record ImportRowResponse(int RowNumber, bool Success, List<string> Errors);
}
