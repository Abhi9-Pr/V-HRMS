using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Helpdesk;
using Vespera.Application.Features.Helpdesk.Routing;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class RaiseTicketCommandHandlerTests
{
    private readonly IReadRepository<TicketCategory> _categories = Substitute.For<IReadRepository<TicketCategory>>();
    private readonly IReadRepository<SlaPolicy> _policies = Substitute.For<IReadRepository<SlaPolicy>>();
    private readonly IReadRepository<PublicHoliday> _holidays = Substitute.For<IReadRepository<PublicHoliday>>();
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly IWriteRepository<Ticket> _tickets = Substitute.For<IWriteRepository<Ticket>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero); // a Monday

    public RaiseTicketCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _holidays.ListAsync(Arg.Any<PublicHolidaysByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private RaiseTicketCommandHandler CreateHandler(IEnumerable<ITicketRoutingRule>? rules = null) => new(
        _categories, _policies, _holidays, _departments, _tickets, _tenantContext, _dateTimeProvider,
        new TicketRoutingEvaluator(rules ?? [new DefaultToDepartmentHeadRoutingRule()]),
        CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));

    private (TicketCategory Category, SlaPolicy Policy, Department Department) SeedCategoryAndPolicy(EmployeeId? headEmployeeId)
    {
        var department = Department.Create(_tenantId, "IT", "IT", null, Now, "admin@vespera.test").Value;
        if (headEmployeeId is { } head)
        {
            department.AssignHead(head, Now, "admin@vespera.test");
        }

        var policy = SlaPolicy.Create(
            _tenantId, "Standard", TimeSpan.FromHours(2), TimeSpan.FromHours(10), Now, "admin@vespera.test",
            new TimeOnly(9, 0), new TimeOnly(18, 0)).Value;

        var category = TicketCategory.Create(_tenantId, "Hardware", department.Id, policy.Id, Now, "admin@vespera.test").Value;

        _categories.FirstOrDefaultAsync(Arg.Any<TicketCategoryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(category);
        _policies.FirstOrDefaultAsync(Arg.Any<SlaPolicyByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(policy);
        _departments.FirstOrDefaultAsync(Arg.Any<DepartmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(department);

        return (category, policy, department);
    }

    [Fact]
    public async Task Handle_Should_Raise_The_Ticket_And_Auto_Assign_To_The_Department_Head()
    {
        var headId = EmployeeId.New();
        var (category, _, _) = SeedCategoryAndPolicy(headId);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RaiseTicketCommand(category.Id.Value, "Laptop broken", "Won't turn on.", TicketPriority.High, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tickets.Received(1).AddAsync(
            Arg.Is<Ticket>(t => t.AssignedTo == headId && t.Status == TicketStatus.InProgress), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Leave_The_Ticket_Unassigned_When_The_Department_Has_No_Head()
    {
        var (category, _, _) = SeedCategoryAndPolicy(headEmployeeId: null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RaiseTicketCommand(category.Id.Value, "Laptop broken", "Won't turn on.", TicketPriority.High, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _tickets.Received(1).AddAsync(Arg.Is<Ticket>(t => t.AssignedTo == null && t.Status == TicketStatus.Open), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Compute_A_Business_Hours_Aware_Due_Date()
    {
        // Monday 09:00 (business open) + 10h resolution: 9h Monday (09:00-18:00), 1h remaining
        // rolls to Tuesday 09:00 -> 10:00.
        var monday0900 = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(monday0900);
        var (category, _, _) = SeedCategoryAndPolicy(headEmployeeId: null);

        var handler = CreateHandler();
        await handler.Handle(new RaiseTicketCommand(category.Id.Value, "Subject", "Description", TicketPriority.Low, null), CancellationToken.None);

        await _tickets.Received(1).AddAsync(
            Arg.Is<Ticket>(t => t.DueAt == new DateTimeOffset(2026, 1, 6, 10, 0, 0, TimeSpan.Zero)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Category_Has_No_Default_Sla_Policy()
    {
        var department = Department.Create(_tenantId, "IT", "IT", null, Now, "admin@vespera.test").Value;
        var category = TicketCategory.Create(_tenantId, "Hardware", department.Id, null, Now, "admin@vespera.test").Value;
        _categories.FirstOrDefaultAsync(Arg.Any<TicketCategoryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(category);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RaiseTicketCommand(category.Id.Value, "Subject", "Description", TicketPriority.Low, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _tickets.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Category_Does_Not_Exist()
    {
        _categories.FirstOrDefaultAsync(Arg.Any<TicketCategoryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((TicketCategory?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RaiseTicketCommand(Guid.NewGuid(), "Subject", "Description", TicketPriority.Low, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
