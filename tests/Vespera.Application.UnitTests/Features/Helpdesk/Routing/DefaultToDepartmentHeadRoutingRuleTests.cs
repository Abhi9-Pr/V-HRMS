using FluentAssertions;
using Vespera.Application.Features.Helpdesk.Routing;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk.Routing;

public class DefaultToDepartmentHeadRoutingRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_Should_Return_The_Department_Head_When_One_Is_Configured()
    {
        var headId = EmployeeId.New();
        var department = Department.Create(TenantId.New(), "IT", "IT", null, Now, "admin@vespera.test").Value;
        department.AssignHead(headId, Now, "admin@vespera.test");

        var result = new DefaultToDepartmentHeadRoutingRule().Evaluate(TicketPriority.Medium, department);

        result.Should().Be(headId);
    }

    [Fact]
    public void Evaluate_Should_Return_Null_When_No_Head_Is_Configured()
    {
        var department = Department.Create(TenantId.New(), "IT", "IT", null, Now, "admin@vespera.test").Value;

        var result = new DefaultToDepartmentHeadRoutingRule().Evaluate(TicketPriority.Medium, department);

        result.Should().BeNull();
    }
}
