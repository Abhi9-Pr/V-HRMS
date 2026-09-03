using FluentAssertions;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

/// <summary>Exercises <see cref="GetAttendanceGridQueryHandler.BuildRows"/> directly — the pure
/// in-memory join/reshape, deliberately factored out of the handler so this doesn't need a mocked
/// IQueryable (which would only prove the mock behaves as configured, never that the projection
/// itself translates correctly — see RunDryRunCommandHandlerTests' own note on why an EF-backed
/// integration test is what actually proves that).</summary>
public class GetAttendanceGridQueryHandlerTests
{
    [Fact]
    public void BuildRows_Should_Produce_One_Day_Entry_Per_Date_In_Range_Even_With_No_Matching_Cells()
    {
        var employee = new AttendanceGridEmployeeProjection(Guid.NewGuid(), "EMP-001", "Ada", "Lovelace");
        var rangeStart = new DateOnly(2026, 6, 1);
        var rangeEnd = new DateOnly(2026, 6, 3);

        var rows = GetAttendanceGridQueryHandler.BuildRows([employee], [], rangeStart, rangeEnd);

        rows.Should().ContainSingle();
        var row = rows[0];
        row.EmployeeId.Should().Be(employee.EmployeeId);
        row.EmployeeName.Should().Be("Ada Lovelace");
        row.Days.Should().HaveCount(3, "the range spans 3 calendar days inclusive");
        row.Days.Should().OnlyContain(day => day.Status == null, "no AttendanceDay cell exists for any of these dates");
        row.Days.Select(day => day.Date).Should().Equal(rangeStart, rangeStart.AddDays(1), rangeStart.AddDays(2));
    }

    [Fact]
    public void BuildRows_Should_Fill_In_The_Status_For_Dates_That_Have_A_Matching_Cell_And_Leave_Others_Null()
    {
        var employee = new AttendanceGridEmployeeProjection(Guid.NewGuid(), "EMP-001", "Ada", "Lovelace");
        var rangeStart = new DateOnly(2026, 6, 1);
        var rangeEnd = new DateOnly(2026, 6, 2);
        var cells = new[] { new AttendanceGridCellProjection(employee.EmployeeId, rangeStart, "Present") };

        var rows = GetAttendanceGridQueryHandler.BuildRows([employee], cells, rangeStart, rangeEnd);

        var row = rows.Should().ContainSingle().Subject;
        row.Days.Should().Contain(day => day.Date == rangeStart && day.Status == "Present");
        row.Days.Should().Contain(day => day.Date == rangeEnd && day.Status == null);
    }

    [Fact]
    public void BuildRows_Should_Never_Cross_Wire_One_Employees_Cells_Onto_Another()
    {
        var employeeOne = new AttendanceGridEmployeeProjection(Guid.NewGuid(), "EMP-001", "Ada", "Lovelace");
        var employeeTwo = new AttendanceGridEmployeeProjection(Guid.NewGuid(), "EMP-002", "Grace", "Hopper");
        var date = new DateOnly(2026, 6, 1);
        var cells = new[] { new AttendanceGridCellProjection(employeeOne.EmployeeId, date, "Present") };

        var rows = GetAttendanceGridQueryHandler.BuildRows([employeeOne, employeeTwo], cells, date, date);

        rows.Single(row => row.EmployeeId == employeeOne.EmployeeId).Days.Single().Status.Should().Be("Present");
        rows.Single(row => row.EmployeeId == employeeTwo.EmployeeId).Days.Single().Status.Should().BeNull();
    }
}
