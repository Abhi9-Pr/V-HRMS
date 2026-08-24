using FluentAssertions;
using Vespera.Application.Features.Helpdesk.Routing;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk.Routing;

public class TicketRoutingEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class AlwaysNullRule : ITicketRoutingRule
    {
        public int Order => 1;

        public EmployeeId? Evaluate(TicketPriority priority, Department department) => null;
    }

    private sealed class AlwaysReturnsRule : ITicketRoutingRule
    {
        private readonly EmployeeId _employeeId;

        public AlwaysReturnsRule(EmployeeId employeeId) => _employeeId = employeeId;

        public int Order => 2;

        public EmployeeId? Evaluate(TicketPriority priority, Department department) => _employeeId;
    }

    [Fact]
    public void Evaluate_Should_Return_The_First_Non_Null_Result_In_Order()
    {
        var expected = EmployeeId.New();
        var evaluator = new TicketRoutingEvaluator([new AlwaysReturnsRule(expected), new AlwaysNullRule()]);
        var department = Department.Create(TenantId.New(), "IT", "IT", null, Now, "admin@vespera.test").Value;

        var result = evaluator.Evaluate(TicketPriority.High, department);

        result.Should().Be(expected);
    }

    [Fact]
    public void Evaluate_Should_Return_Null_When_Every_Rule_Defers()
    {
        var evaluator = new TicketRoutingEvaluator([new AlwaysNullRule()]);
        var department = Department.Create(TenantId.New(), "IT", "IT", null, Now, "admin@vespera.test").Value;

        var result = evaluator.Evaluate(TicketPriority.High, department);

        result.Should().BeNull();
    }
}
