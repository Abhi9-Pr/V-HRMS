using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the employee-documents slice (M3: upload + virus scan + verify/reject +
/// signed download URL) works end to end against the real pipeline, using the real
/// NullVirusScanner (see appsettings — no VirusScan:Provider override needed in test
/// environments, since the default is already "Null").</summary>
public class EmployeeDocumentsCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VesperaWebApplicationFactory _factory;

    public EmployeeDocumentsCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_List_GetDownloadUrl_Verify_Reject_Delete_Should_Round_Trip()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-employee-documents");

        var employeeId = await CreateEmployeeAsync(client);

        using var uploadContent = new MultipartFormDataContent();
        var fileBytes = "not a real image, just test bytes"u8.ToArray();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "File", "passport.jpg");
        uploadContent.Add(new StringContent("Id"), "DocumentType");

        var uploadResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/documents", uploadContent);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK, uploadBody);
        var uploaded = JsonSerializer.Deserialize<UploadResponse>(uploadBody, JsonOptions);
        var documentId = uploaded!.Id;

        var listResponse = await client.GetAsync($"/api/v1/employees/{employeeId}/documents");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var documents = await listResponse.Content.ReadFromJsonAsync<List<DocumentResponse>>();
        documents.Should().ContainSingle(d => d.Id == documentId && d.ScanStatus == "Clean");

        var downloadUrlResponse = await client.GetAsync($"/api/v1/employees/{employeeId}/documents/{documentId}/download-url");
        downloadUrlResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadUrl = await downloadUrlResponse.Content.ReadFromJsonAsync<DownloadUrlResponse>();
        downloadUrl!.Url.Should().NotBeNullOrWhiteSpace();

        var verifyResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/documents/{documentId}/verify", null);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteResponse = await client.DeleteAsync($"/api/v1/employees/{employeeId}/documents/{documentId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterDeleteResponse = await client.GetAsync($"/api/v1/employees/{employeeId}/documents");
        var documentsAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<List<DocumentResponse>>();
        documentsAfterDelete.Should().NotContain(d => d.Id == documentId);
    }

    [Fact]
    public async Task Reject_Should_Set_VerificationStatus_And_Reason()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-employee-documents-reject");

        var employeeId = await CreateEmployeeAsync(client);

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("more test bytes"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "File", "id-card.jpg");
        uploadContent.Add(new StringContent("Id"), "DocumentType");
        var uploadResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/documents", uploadContent);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>();

        var rejectResponse = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeId}/documents/{uploaded!.Id}/reject", new { reason = "Blurry scan" });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync($"/api/v1/employees/{employeeId}/documents");
        var documents = await listResponse.Content.ReadFromJsonAsync<List<DocumentResponse>>();
        documents.Should().ContainSingle(d => d.Id == uploaded.Id && d.VerificationStatus == "Rejected");
    }

    [Fact]
    public async Task List_Should_Return_Forbidden_For_A_User_Without_EmployeeDocuments_Read()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-employee-documents-forbidden");

        var response = await client.GetAsync($"/api/v1/employees/{Guid.NewGuid()}/documents");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDownloadUrl_Should_Return_NotFound_For_A_Document_Owned_By_Another_Tenant()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-employee-documents-idor-owner");

        var employeeId = await CreateEmployeeAsync(client);

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("idor test bytes"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "File", "passport.jpg");
        uploadContent.Add(new StringContent("Id"), "DocumentType");
        var uploadResponse = await client.PostAsync($"/api/v1/employees/{employeeId}/documents", uploadContent);
        var documentId = (await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        // Cross-tenant employeeId means the parent lookup itself must fail before the document
        // list/download-url is even reachable — confirmed 404 on both, not just the single-document path.
        (await otherTenantClient.GetAsync($"/api/v1/employees/{employeeId}/documents/{documentId}/download-url"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherTenantClient.GetAsync($"/api/v1/employees/{employeeId}/documents"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> CreateEmployeeAsync(HttpClient client)
    {
        var departmentResponse = await client.PostAsJsonAsync(
            "/api/v1/departments", new { name = "R&D", code = $"RND-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Researcher-{Guid.NewGuid():N}", grade = 3 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new
            {
                name = "R&D Campus",
                addressLine = "2 Innovation Way",
                city = "Pune",
                country = "India",
                latitude = 18.5204,
                longitude = 73.8567,
                timeZoneId = "Asia/Kolkata",
            });
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
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.Id;
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record UploadResponse(Guid Id);

    private sealed record DocumentResponse(Guid Id, string DocumentType, string ScanStatus, string VerificationStatus, string? RejectionReason);

    private sealed record DownloadUrlResponse(string Url, DateTimeOffset ExpiresAt);
}
