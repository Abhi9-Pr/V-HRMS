using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class UploadEmployeeDocumentCommandValidatorTests
{
    private readonly UploadEmployeeDocumentCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(
            new UploadEmployeeDocumentCommand(Guid.NewGuid(), EmployeeDocumentType.Id, "passport.jpg", [1, 2, 3]));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(
            new UploadEmployeeDocumentCommand(Guid.Empty, EmployeeDocumentType.Id, "passport.jpg", [1]));
        result.ShouldHaveValidationErrorFor(command => command.EmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var result = _validator.TestValidate(
            new UploadEmployeeDocumentCommand(Guid.NewGuid(), EmployeeDocumentType.Id, "", [1]));
        result.ShouldHaveValidationErrorFor(command => command.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        var result = _validator.TestValidate(
            new UploadEmployeeDocumentCommand(Guid.NewGuid(), EmployeeDocumentType.Id, "passport.jpg", []));
        result.ShouldHaveValidationErrorFor(command => command.Content);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Exceeds_Max_Size()
    {
        var oversized = new byte[20 * 1024 * 1024 + 1];
        var result = _validator.TestValidate(
            new UploadEmployeeDocumentCommand(Guid.NewGuid(), EmployeeDocumentType.Id, "passport.jpg", oversized));
        result.ShouldHaveValidationErrorFor(command => command.Content);
    }
}
