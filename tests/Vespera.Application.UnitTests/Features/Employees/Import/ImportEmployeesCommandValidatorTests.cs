using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees.Import;

namespace Vespera.Application.UnitTests.Features.Employees.Import;

public class ImportEmployeesCommandValidatorTests
{
    private readonly ImportEmployeesCommandValidator _validator = new();

    private static ImportEmployeeRowDto CreateRow(int rowNumber) => new(
        rowNumber, "EMP-001", "Priya", "Sharma", "priya.sharma@demo.vespera.test", "+919876543210",
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), "ENG", "Software Engineer", "Head Office");

    [Fact]
    public void Should_Have_Error_When_Rows_Is_Empty()
    {
        var result = _validator.TestValidate(new ImportEmployeesCommand([], DryRun: false));

        result.ShouldHaveValidationErrorFor(c => c.Rows);
    }

    [Fact]
    public void Should_Have_Error_When_A_Row_Number_Is_Not_Positive()
    {
        var result = _validator.TestValidate(new ImportEmployeesCommand([CreateRow(0)], DryRun: false));

        result.ShouldHaveValidationErrorFor("Rows[0].RowNumber");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ImportEmployeesCommand([CreateRow(1), CreateRow(2)], DryRun: true));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
