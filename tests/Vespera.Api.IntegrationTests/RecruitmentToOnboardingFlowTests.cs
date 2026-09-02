using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>
/// End-to-end proof of Phase 11c: a requisition goes Draft -> submitted -> approved (by the
/// submitter's resolved manager, the same generic approval-chain engine Leave/Expenses use) ->
/// published, and is then visible on the genuinely anonymous /api/v1/public/jobs endpoint (no
/// bearer token at all) scoped correctly by tenant. A candidate is created against it, the
/// stage-transition rule blocks moving into the "Offer" stage before any interview is completed,
/// an interview is scheduled and completed, the move then succeeds, an offer letter is created,
/// sent, downloaded as a real PDF, accepted, and the candidate is converted into an Employee with
/// no re-keying of name/email/phone/designation/department/joining-date.
/// </summary>
public class RecruitmentToOnboardingFlowTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public RecruitmentToOnboardingFlowTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Requisition_Through_Approval_Publish_And_Candidate_Through_Hire_Works_End_To_End()
    {
        var (rohanClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-recruitment");

        // Vikram is Finance.Admin, TOTP-enrolled — see ExpenseSettlementFlowTests for the same pattern.
        var vikramTotpCode = TotpTestHelper.ComputeCurrentCode(DevelopmentSeeder.FinanceAdminTotpSecretBase32);
        var (vikramClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "vikram.nair@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-vikram-recruitment", vikramTotpCode);

        var engineeringDepartmentId = await GetDepartmentIdByCodeAsync("ENG");
        var softwareEngineerDesignationId = await GetDesignationIdByTitleAsync("Software Engineer");
        var headOfficeLocationId = await GetLocationIdByNameAsync("Head Office");
        var rohanEmployeeId = await GetEmployeeIdByEmailAsync("rohan.verma@demo.vespera.test");

        // 1. Requisition: create -> add stages -> submit -> approve (Vikram, Rohan's resolved
        //    approver) -> publish.
        var createRequisitionResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/recruitment/requisitions",
            new { title = $"Senior Engineer {Guid.NewGuid():N}", departmentId = engineeringDepartmentId, openingsCount = 1 });
        createRequisitionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var requisitionId = (await createRequisitionResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        (await rohanClient.PostAsJsonAsync($"/api/v1/recruitment/requisitions/{requisitionId}/stages", new { stageName = "Screening" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await rohanClient.PostAsJsonAsync($"/api/v1/recruitment/requisitions/{requisitionId}/stages", new { stageName = "Offer" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var requisitionDetail = await GetRequisitionAsync(rohanClient, requisitionId);
        var screeningStageId = requisitionDetail.Stages.Single(s => s.Name == "Screening").Id;
        var offerStageId = requisitionDetail.Stages.Single(s => s.Name == "Offer").Id;

        (await rohanClient.PostAsync($"/api/v1/recruitment/requisitions/{requisitionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var decideResponse = await vikramClient.PostAsJsonAsync(
            $"/api/v1/recruitment/requisitions/{requisitionId}/decision", new { approved = true, comment = "Budget approved" });
        decideResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await rohanClient.PostAsync($"/api/v1/recruitment/requisitions/{requisitionId}/publish", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 2. Public jobs feed: genuinely anonymous (no bearer token at all), tenant-scoped, and
        //    cached (two identical calls with the same tenant header return identical bodies).
        var demoTenantId = await _factory.GetDemoTenantIdAsync();

        var anonymousClient = _factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Tenant-Id", demoTenantId.ToString());

        var firstPublicResponse = await anonymousClient.GetAsync("/api/v1/public/jobs");
        firstPublicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await firstPublicResponse.Content.ReadAsStringAsync();
        var publicJobs = await firstPublicResponse.Content.ReadFromJsonAsync<List<PublicJobResponse>>()
            ?? throw new InvalidOperationException();
        publicJobs.Should().Contain(job => job.Id == requisitionId);

        var secondPublicResponse = await anonymousClient.GetAsync("/api/v1/public/jobs");
        var secondBody = await secondPublicResponse.Content.ReadAsStringAsync();
        secondBody.Should().Be(firstBody);

        var wrongTenantClient = _factory.CreateClient();
        wrongTenantClient.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        var wrongTenantResponse = await wrongTenantClient.GetAsync("/api/v1/public/jobs");
        wrongTenantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await wrongTenantResponse.Content.ReadFromJsonAsync<List<PublicJobResponse>>()).Should().BeEmpty();

        // 3. Candidate: create -> blocked from the "Offer" stage before any completed interview
        //    -> interview scheduled + completed -> the same move now succeeds.
        var createCandidateResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/recruitment/candidates",
            new { jobRequisitionId = requisitionId, fullName = "Jordan Lee", email = "jordan.lee@example.com", phone = "+14155552671" });
        createCandidateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var candidateId = (await createCandidateResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var blockedMoveResponse = await rohanClient.PostAsJsonAsync(
            $"/api/v1/recruitment/candidates/{candidateId}/stage", new { targetStageId = offerStageId });
        blockedMoveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await rohanClient.PostAsJsonAsync($"/api/v1/recruitment/candidates/{candidateId}/stage", new { targetStageId = screeningStageId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var scheduleInterviewResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/recruitment/interviews",
            new
            {
                candidateId,
                pipelineStageId = screeningStageId,
                scheduledAt = DateTimeOffset.UtcNow.AddDays(2),
                interviewerIds = new[] { rohanEmployeeId },
            });
        scheduleInterviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var interviewId = (await scheduleInterviewResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        (await rohanClient.PostAsJsonAsync(
                $"/api/v1/recruitment/interviews/{interviewId}/complete", new { feedback = "Strong technical round", rating = 5 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await rohanClient.PostAsJsonAsync($"/api/v1/recruitment/candidates/{candidateId}/stage", new { targetStageId = offerStageId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Offer letter: create -> send -> download a real PDF -> accept -> convert to employee
        //    with no re-keying of name/email/phone/designation/department/joining-date.
        var joiningDate = new DateOnly(2026, 6, 1);
        var createOfferResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/recruitment/offers",
            new
            {
                candidateId,
                proposedDesignationId = softwareEngineerDesignationId,
                proposedCtc = 1_800_000m,
                currency = Currency.Inr,
                joiningDate,
            });
        createOfferResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var offerLetterId = (await createOfferResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        (await rohanClient.PostAsync($"/api/v1/recruitment/offers/{offerLetterId}/send", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var pdfResponse = await rohanClient.GetAsync($"/api/v1/recruitment/offers/{offerLetterId}/pdf");
        pdfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);
        pdfText.Should().StartWith("%PDF-1.");
        pdfText.TrimEnd().Should().EndWith("%%EOF");

        (await rohanClient.PostAsync($"/api/v1/recruitment/offers/{offerLetterId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var dateOfBirth = new DateOnly(1996, 7, 22);
        var employeeCode = $"EMP-{Guid.NewGuid():N}"[..12];
        var convertResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/recruitment/offers/convert-to-employee",
            new { candidateId, offerLetterId, employeeCode, dateOfBirth, locationId = headOfficeLocationId });
        convertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newEmployeeId = (await convertResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var candidateAfter = await rohanClient.GetFromJsonAsync<CandidateResponse>($"/api/v1/recruitment/candidates/{candidateId}");
        candidateAfter!.Status.Should().Be(3); // CandidateStatus.Hired

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var newEmployee = await dbContext.Set<Employee>().IgnoreQueryFilters().SingleAsync(e => e.Id == new EmployeeId(newEmployeeId));

        newEmployee.FirstName.Should().Be("Jordan");
        newEmployee.LastName.Should().Be("Lee");
        newEmployee.WorkEmail.Value.Should().Be("jordan.lee@example.com");
        newEmployee.Phone.Value.Should().Be("+14155552671");
        newEmployee.DepartmentId.Should().Be(new DepartmentId(engineeringDepartmentId));
        newEmployee.DesignationId.Should().Be(new DesignationId(softwareEngineerDesignationId));
        newEmployee.DateOfJoining.Should().Be(joiningDate);
    }

    [Fact]
    public async Task Get_Should_Return_NotFound_For_A_Requisition_Owned_By_Another_Tenant()
    {
        var (rohanClient, _) = await _factory.CreateAuthenticatedClientAsync(
            "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-recruitment-idor");

        var engineeringDepartmentId = await GetDepartmentIdByCodeAsync("ENG");

        var createRequisitionResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/recruitment/requisitions",
            new { title = $"Cross-Tenant Target {Guid.NewGuid():N}", departmentId = engineeringDepartmentId, openingsCount = 1 });
        createRequisitionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var requisitionId = (await createRequisitionResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        var response = await otherTenantClient.GetAsync($"/api/v1/recruitment/requisitions/{requisitionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<JobRequisitionResponse> GetRequisitionAsync(HttpClient client, Guid requisitionId) =>
        await client.GetFromJsonAsync<JobRequisitionResponse>($"/api/v1/recruitment/requisitions/{requisitionId}")
        ?? throw new InvalidOperationException();

    private async Task<Guid> GetDepartmentIdByCodeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var department = await dbContext.Set<Department>().IgnoreQueryFilters().SingleAsync(d => d.Code == code);
        return department.Id.Value;
    }

    private async Task<Guid> GetDesignationIdByTitleAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var designation = await dbContext.Set<Designation>().IgnoreQueryFilters().SingleAsync(d => d.Title == title);
        return designation.Id.Value;
    }

    private async Task<Guid> GetLocationIdByNameAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var location = await dbContext.Set<Location>().IgnoreQueryFilters().SingleAsync(l => l.Name == name);
        return location.Id.Value;
    }

    private async Task<Guid> GetEmployeeIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var emailAddress = EmailAddress.Create(email).Value;
        var employee = await dbContext.Set<Employee>().IgnoreQueryFilters().FirstAsync(e => e.WorkEmail == emailAddress);
        return employee.Id.Value;
    }

    private sealed record IdResponse(Guid Id);

    private sealed record PipelineStageResponse(Guid Id, string Name, int SequenceNumber);

    private sealed record JobRequisitionResponse(Guid Id, string Title, IReadOnlyList<PipelineStageResponse> Stages);

    private sealed record PublicJobResponse(Guid Id, string Title, string DepartmentName, int OpeningsCount);

    private sealed record CandidateResponse(Guid Id, Guid JobRequisitionId, string FullName, string Email, string Phone, int Status);
}
