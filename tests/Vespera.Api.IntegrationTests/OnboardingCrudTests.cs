using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>Proves the onboarding pipeline (M6) works end to end against the real pipeline:
/// start a draft, fill both steps, upload an ID document (real virus-scan path via
/// NullVirusScanner), run OCR (via VesperaWebApplicationFactory's FakeDocumentOcrService — see
/// its doc comment for why), confirm only the explicitly-supplied fields, record consent, submit,
/// and confirm the resulting employee is real and visible over HTTP.</summary>
public class OnboardingCrudTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public OnboardingCrudTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Onboard_Should_Chain_Start_Through_Submit_And_Produce_A_Real_Employee()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-onboarding");

        var (departmentId, designationId, locationId) = await CreateMastersAsync(client);

        var startResponse = await client.PostAsync("/api/v1/onboarding", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK, await startResponse.Content.ReadAsStringAsync());
        var draft = await startResponse.Content.ReadFromJsonAsync<IdResponse>();
        var draftId = draft!.Id;

        var personalResponse = await client.PutAsJsonAsync($"/api/v1/onboarding/{draftId}/personal-details", new
        {
            firstName = "Placeholder",
            lastName = "Placeholder",
            workEmail = $"newhire.{Guid.NewGuid():N}@vespera.test",
            phone = "+14155552672",
            dateOfBirth = "1992-05-01",
        });
        personalResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await personalResponse.Content.ReadAsStringAsync());

        var employmentResponse = await client.PutAsJsonAsync($"/api/v1/onboarding/{draftId}/employment-details", new
        {
            departmentId,
            designationId,
            locationId,
            dateOfJoining = "2026-03-01",
        });
        employmentResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await employmentResponse.Content.ReadAsStringAsync());

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("not a real id image, just test bytes"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "File", "id-card.jpg");
        uploadContent.Add(new StringContent("Id"), "DocumentType");
        var uploadResponse = await client.PostAsync($"/api/v1/onboarding/{draftId}/documents", uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK, await uploadResponse.Content.ReadAsStringAsync());
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<IdResponse>();
        var documentId = uploaded!.Id;

        // Extracting before consent is recorded must be rejected outright.
        var extractBeforeConsentResponse = await client.PostAsync($"/api/v1/onboarding/{draftId}/documents/{documentId}/extract", null);
        extractBeforeConsentResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var consentResponse = await client.PostAsJsonAsync($"/api/v1/onboarding/{draftId}/consent", new { consentType = "DataProcessing" });
        consentResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await consentResponse.Content.ReadAsStringAsync());

        var extractResponse = await client.PostAsync($"/api/v1/onboarding/{draftId}/documents/{documentId}/extract", null);
        extractResponse.StatusCode.Should().Be(HttpStatusCode.OK, await extractResponse.Content.ReadAsStringAsync());
        var extraction = await extractResponse.Content.ReadFromJsonAsync<OcrExtractionResponse>();
        extraction!.Fields["firstName"].Should().Be("OcrSuggested");

        // Confirm with a value that deliberately differs from what the fake OCR suggested, to
        // prove confirmation applies what the caller passed, not the raw suggestion.
        var confirmResponse = await client.PostAsJsonAsync(
            $"/api/v1/onboarding/{draftId}/documents/{documentId}/confirm",
            new { confirmedFirstName = "Grace", confirmedLastName = "Hopper" });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, await confirmResponse.Content.ReadAsStringAsync());

        var employeeCode = $"EMP-{Guid.NewGuid():N}"[..12];
        var submitResponse = await client.PostAsJsonAsync($"/api/v1/onboarding/{draftId}/submit", new { employeeCode });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, await submitResponse.Content.ReadAsStringAsync());
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitResponse>();

        var employeeResponse = await client.GetAsync($"/api/v1/employees/{submitted!.EmployeeId}");
        employeeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var employee = await employeeResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        employee!.FirstName.Should().Be("Grace");
        employee.LastName.Should().Be("Hopper");
        employee.Code.Should().Be(employeeCode);

        var draftAfterSubmitResponse = await client.GetAsync($"/api/v1/onboarding/{draftId}");
        var draftAfterSubmit = await draftAfterSubmitResponse.Content.ReadFromJsonAsync<DraftResponse>();
        draftAfterSubmit!.Status.Should().Be("Converted");
        draftAfterSubmit.ConvertedEmployeeId.Should().Be(submitted.EmployeeId);
    }

    [Fact]
    public async Task Start_Should_Return_Forbidden_For_A_User_Without_Onboarding_Manage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync(
            "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-onboarding-forbidden");

        var response = await client.PostAsync("/api/v1/onboarding", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<(Guid DepartmentId, Guid DesignationId, Guid LocationId)> CreateMastersAsync(HttpClient client)
    {
        var departmentResponse = await client.PostAsJsonAsync(
            "/api/v1/departments", new { name = "Engineering", code = $"ENG-{Guid.NewGuid():N}"[..16] });
        var department = await departmentResponse.Content.ReadFromJsonAsync<IdResponse>();

        var designationResponse = await client.PostAsJsonAsync(
            "/api/v1/designations", new { title = $"Engineer-{Guid.NewGuid():N}", grade = 4 });
        var designation = await designationResponse.Content.ReadFromJsonAsync<IdResponse>();

        var locationResponse = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new
            {
                name = "HQ",
                addressLine = "1 Main Street",
                city = "Bengaluru",
                country = "India",
                latitude = 12.9716,
                longitude = 77.5946,
                timeZoneId = "Asia/Kolkata",
            });
        var location = await locationResponse.Content.ReadFromJsonAsync<IdResponse>();

        return (department!.Id, designation!.Id, location!.Id);
    }

    private sealed record IdResponse(Guid Id);

    private sealed record SubmitResponse(Guid EmployeeId);

    private sealed record OcrExtractionResponse(string ExtractedText, Dictionary<string, string> Fields, double Confidence);

    private sealed record EmployeeResponse(Guid Id, string Code, string FirstName, string LastName);

    private sealed record DraftResponse(Guid Id, string Status, Guid? ConvertedEmployeeId);
}
