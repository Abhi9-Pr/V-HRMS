using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetSlaComplianceReportQueryHandlerTests
{
    private readonly IVesperaDbContext _dbContext = Substitute.For<IVesperaDbContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    public GetSlaComplianceReportQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetSlaComplianceReportQueryHandler CreateHandler() => new(_dbContext, _tenantContext);

    private TicketCategory CreateCategory(string name) =>
        TicketCategory.Create(_tenantId, name, DepartmentId.New(), null, Now, "admin@vespera.test").Value;

    private Ticket RaiseResolvedTicket(TicketCategory category, TimeSpan resolutionTime, DateTimeOffset resolveAt, bool breached)
    {
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), category.Id, SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, resolutionTime).Value;

        if (breached)
        {
            ticket.CheckSlaBreach(resolveAt);
        }

        ticket.Resolve(resolveAt);
        return ticket;
    }

    [Fact]
    public async Task Handle_Should_Aggregate_Compliance_Across_Resolved_And_Closed_Tickets()
    {
        var category = CreateCategory("Hardware");
        var onTime = RaiseResolvedTicket(category, TimeSpan.FromHours(24), Now.AddHours(1), breached: false);
        var breachedTicket = RaiseResolvedTicket(category, TimeSpan.FromHours(1), Now.AddHours(2), breached: true);
        var openTicket = Ticket.Raise(
            _tenantId, EmployeeId.New(), category.Id, SlaPolicyId.New(), "Untouched", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;

        _dbContext.Set<Ticket>().Returns(new List<Ticket> { onTime, breachedTicket, openTicket }.AsQueryable());
        _dbContext.Set<TicketCategory>().Returns(new List<TicketCategory> { category }.AsQueryable());

        var result = await CreateHandler().Handle(new GetSlaComplianceReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalResolvedOrClosed.Should().Be(2);
        result.Value.BreachedCount.Should().Be(1);
        result.Value.OnTimeCount.Should().Be(1);
        result.Value.CompliancePercentage.Should().Be(50.0);
        result.Value.ByCategory.Should().ContainSingle(row =>
            row.CategoryId == category.Id.Value && row.CategoryName == "Hardware" && row.Total == 2 && row.Breached == 1);
    }

    [Fact]
    public async Task Handle_Should_Return_Full_Compliance_When_There_Are_No_Resolved_Tickets()
    {
        _dbContext.Set<Ticket>().Returns(new List<Ticket>().AsQueryable());
        _dbContext.Set<TicketCategory>().Returns(new List<TicketCategory>().AsQueryable());

        var result = await CreateHandler().Handle(new GetSlaComplianceReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalResolvedOrClosed.Should().Be(0);
        result.Value.CompliancePercentage.Should().Be(100.0);
        result.Value.ByCategory.Should().BeEmpty();
    }
}
