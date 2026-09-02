using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;

namespace Vespera.Api.IntegrationTests;

/// <summary>End-to-end proof of Phase 11d: business-hours-aware SLA due dates that genuinely
/// respect the holiday calendar (not just weekends), automatic breach escalation to the
/// department head via the notification fan-out, threaded comments with an internal-only note and
/// an attachment, and the SLA compliance report reflecting the breach. Time and the notification
/// dispatcher are both overridden for this test only (via <c>WithWebHostBuilder</c> on the shared
/// fixture) — required for a deterministic due-date assertion and for observing the escalation
/// without a real email/SMS provider; no other test in this suite is affected.</summary>
public class HelpdeskSlaEscalationFlowTests : IClassFixture<VesperaWebApplicationFactory>
{
    private readonly VesperaWebApplicationFactory _factory;

    public HelpdeskSlaEscalationFlowTests(VesperaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sla_Breach_Escalates_To_The_Department_Head_And_Reflects_In_The_Compliance_Report()
    {
        var tenantId = await _factory.GetDemoTenantIdAsync();
        var timeProvider = new MutableDateTimeProvider { UtcNow = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero) };
        var notificationDispatcher = new RecordingNotificationDispatcher();

        var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDateTimeProvider>(timeProvider);
            services.AddSingleton<INotificationDispatcher>(notificationDispatcher);
        }));

        var rohanClient = await CreateAuthenticatedClientAsync(
            factory, tenantId, "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-helpdesk");
        var priyaClient = await CreateAuthenticatedClientAsync(
            factory, tenantId, "priya.sharma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-priya-helpdesk");

        var hrDepartmentId = await GetDepartmentIdByCodeAsync("HR");
        var rohanEmployeeId = await GetEmployeeIdByEmailAsync("rohan.verma@demo.vespera.test");

        // A holiday that falls exactly where a naive (weekend-only) calculation would have landed
        // the due date, so a wrong implementation would silently produce the wrong DueAt below.
        var createHolidayResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/helpdesk/public-holidays", new { date = new DateOnly(2026, 1, 2), name = "Test Holiday" });
        createHolidayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createPolicyResponse = await rohanClient.PostAsJsonAsync("/api/v1/helpdesk/sla-policies", new
        {
            name = "Standard",
            responseTimeHours = 2,
            resolutionTimeHours = 10,
            businessHoursStart = new TimeOnly(9, 0),
            businessHoursEnd = new TimeOnly(18, 0),
        });
        createPolicyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var slaPolicyId = (await createPolicyResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var createCategoryResponse = await rohanClient.PostAsJsonAsync(
            "/api/v1/helpdesk/ticket-categories", new { name = "Hardware", departmentId = hrDepartmentId, defaultSlaPolicyId = slaPolicyId });
        createCategoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryId = (await createCategoryResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var raiseTicketResponse = await priyaClient.PostAsJsonAsync("/api/v1/helpdesk/tickets", new
        {
            categoryId, subject = "Laptop not booting", description = "Won't power on.", priority = TicketPriority.High,
        });
        raiseTicketResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticketId = (await raiseTicketResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var ticketAfterRaise = await GetTicketAsync(priyaClient, ticketId);
        ticketAfterRaise.AssignedTo.Should().Be(rohanEmployeeId);

        // Thu 2026-01-01 09:00 + 10 business hours: 9h Thursday (09:00-18:00), 1h remaining. Friday
        // 2026-01-02 is the configured holiday above (skipped entirely, not just the weekend), so
        // it rolls past Sat/Sun straight to Monday 2026-01-05 09:00 -> 10:00. A weekend-only
        // implementation would have produced Friday 2026-01-02 10:00 instead — three days earlier.
        ticketAfterRaise.DueAt.Should().Be(new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero));

        using var attachmentContent = new MultipartFormDataContent();
        var fileBytes = new ByteArrayContent([1, 2, 3, 4]);
        fileBytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        attachmentContent.Add(fileBytes, "file", "photo.png");
        var uploadResponse = await priyaClient.PostAsync($"/api/v1/helpdesk/tickets/{ticketId}/attachments", attachmentContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachmentReference = (await uploadResponse.Content.ReadFromJsonAsync<AttachmentReferenceResponse>())!.Reference;

        var addCommentResponse = await priyaClient.PostAsJsonAsync($"/api/v1/helpdesk/tickets/{ticketId}/comments", new
        {
            body = "Here is a photo of the issue", isInternal = false, parentCommentId = (Guid?)null, attachmentReferences = new[] { attachmentReference },
        });
        addCommentResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ticketAfterComment = await GetTicketAsync(priyaClient, ticketId);
        var topLevelCommentId = ticketAfterComment.Comments.Single().Id;

        var addReplyResponse = await rohanClient.PostAsJsonAsync($"/api/v1/helpdesk/tickets/{ticketId}/comments", new
        {
            body = "Escalating to IT internally", isInternal = true, parentCommentId = topLevelCommentId, attachmentReferences = (string[]?)null,
        });
        addReplyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ticketAfterReply = await GetTicketAsync(priyaClient, ticketId);
        ticketAfterReply.Comments.Should().ContainSingle(c => c.ParentCommentId == topLevelCommentId && c.IsInternal);

        // Advance past the computed (holiday-aware) due date and run the SLA sweep + outbox.
        timeProvider.UtcNow = new DateTimeOffset(2026, 1, 5, 11, 0, 0, TimeSpan.Zero);
        await RunSlaSweepAsync(factory);
        await DrainOutboxAsync(factory);

        notificationDispatcher.Messages.Should().ContainSingle(
            m => m.RecipientId == rohanEmployeeId.ToString() && m.Title == "SLA breached");

        var resolveResponse = await rohanClient.PostAsync($"/api/v1/helpdesk/tickets/{ticketId}/resolve", null);
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var closeResponse = await rohanClient.PostAsync($"/api/v1/helpdesk/tickets/{ticketId}/close", null);
        closeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rateResponse = await priyaClient.PostAsJsonAsync($"/api/v1/helpdesk/tickets/{ticketId}/satisfaction", new { rating = 3 });
        rateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reportResponse = await rohanClient.GetAsync("/api/v1/helpdesk/tickets/sla-compliance-report");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await reportResponse.Content.ReadFromJsonAsync<SlaComplianceReportResponse>();
        report!.BreachedCount.Should().Be(1);
        report.ByCategory.Should().ContainSingle(c => c.CategoryId == categoryId && c.Breached == 1);
    }

    [Fact]
    public async Task Get_Should_Return_NotFound_For_A_Ticket_Owned_By_Another_Tenant()
    {
        var tenantId = await _factory.GetDemoTenantIdAsync();
        var rohanClient = await CreateAuthenticatedClientAsync(
            _factory, tenantId, "rohan.verma@demo.vespera.test", DevelopmentSeeder.DemoPassword, "device-rohan-helpdesk-idor");

        var hrDepartmentId = await GetDepartmentIdByCodeAsync("HR");

        var createPolicyResponse = await rohanClient.PostAsJsonAsync("/api/v1/helpdesk/sla-policies", new
        {
            name = $"IdorPolicy-{Guid.NewGuid():N}"[..20],
            responseTimeHours = 2,
            resolutionTimeHours = 10,
            businessHoursStart = new TimeOnly(9, 0),
            businessHoursEnd = new TimeOnly(18, 0),
        });
        createPolicyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var slaPolicyId = (await createPolicyResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var createCategoryResponse = await rohanClient.PostAsJsonAsync("/api/v1/helpdesk/ticket-categories", new
        {
            name = $"IdorCategory-{Guid.NewGuid():N}"[..20], departmentId = hrDepartmentId, defaultSlaPolicyId = slaPolicyId,
        });
        createCategoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryId = (await createCategoryResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var raiseTicketResponse = await rohanClient.PostAsJsonAsync("/api/v1/helpdesk/tickets", new
        {
            categoryId, subject = "IDOR probe", description = "Cross-tenant ticket access test.", priority = TicketPriority.Low,
        });
        raiseTicketResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticketId = (await raiseTicketResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var otherTenantClient = await _factory.CreateSecondTenantAdminClientAsync();

        var getAttempt = await otherTenantClient.GetAsync($"/api/v1/helpdesk/tickets/{ticketId}");
        getAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(
        WebApplicationFactory<Program> factory, Guid tenantId, string email, string password, string deviceId, string? totpCode = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password, deviceId, totpCode });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>() ?? throw new InvalidOperationException("Login did not return a body.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<TicketResponse> GetTicketAsync(HttpClient client, Guid ticketId)
    {
        var response = await client.GetAsync($"/api/v1/helpdesk/tickets/{ticketId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TicketResponse>())!;
    }

    private static async Task RunSlaSweepAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var sweep = new SlaEscalationHostedService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BackgroundJobsOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<SlaEscalationHostedService>>());

        await sweep.RunOnceAsync(CancellationToken.None);
    }

    private static async Task DrainOutboxAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var dispatcher = new OutboxDispatcherHostedService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BackgroundJobsOptions()),
            scope.ServiceProvider.GetRequiredService<ILogger<OutboxDispatcherHostedService>>());

        await dispatcher.ProcessOnceAsync(CancellationToken.None);
    }

    private async Task<Guid> GetDepartmentIdByCodeAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var department = await dbContext.Set<Department>().IgnoreQueryFilters().FirstAsync(d => d.Code == code);
        return department.Id.Value;
    }

    private async Task<Guid> GetEmployeeIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var emailAddress = EmailAddress.Create(email).Value;
        var employee = await dbContext.Set<Employee>().IgnoreQueryFilters().FirstAsync(e => e.WorkEmail == emailAddress);
        return employee.Id.Value;
    }

    private sealed class MutableDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class RecordingNotificationDispatcher : INotificationDispatcher
    {
        public List<NotificationMessage> Messages { get; } = [];

        public Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed record IdResponse(Guid Id);

    private sealed record AttachmentReferenceResponse(string Reference);

    private sealed record TicketResponse(
        Guid Id, string Subject, string Description, int Priority, int Status, Guid CategoryId, Guid RaisedBy, Guid? AssignedTo,
        DateTimeOffset RaisedAt, DateTimeOffset DueAt, DateTimeOffset? ResolvedAt, bool SlaBreachNotified, bool SlaWarningNotified,
        int? SatisfactionRating, IReadOnlyList<TicketCommentResponse> Comments);

    private sealed record TicketCommentResponse(
        Guid Id, Guid AuthorId, string Body, bool IsInternal, DateTimeOffset CreatedAt, Guid? ParentCommentId,
        IReadOnlyList<string> AttachmentReferences);

    private sealed record SlaComplianceReportResponse(
        int TotalResolvedOrClosed, int BreachedCount, int OnTimeCount, double CompliancePercentage,
        IReadOnlyList<CategoryComplianceRowResponse> ByCategory);

    private sealed record CategoryComplianceRowResponse(Guid CategoryId, string CategoryName, int Total, int Breached, double CompliancePercentage);
}
