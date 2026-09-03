using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CreateAnnouncementCommandValidatorTests
{
    private static readonly DateTimeOffset PublishAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly CreateAnnouncementCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var result = _validator.TestValidate(CreateCommand() with { Title = string.Empty });

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(CreateCommand() with { Title = new string('a', 257) });

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Body_Is_Empty()
    {
        var result = _validator.TestValidate(CreateCommand() with { Body = string.Empty });

        result.ShouldHaveValidationErrorFor(command => command.Body);
    }

    [Fact]
    public void Should_Have_Error_When_ExpiresAt_Is_Not_After_PublishAt()
    {
        var result = _validator.TestValidate(CreateCommand() with { ExpiresAt = PublishAt.AddDays(-1) });

        result.ShouldHaveValidationErrorFor(command => command.ExpiresAt);
    }

    [Fact]
    public void Should_Have_Error_When_Department_Scoped_Without_A_Target_Department()
    {
        var result = _validator.TestValidate(
            CreateCommand() with { AudienceScope = AnnouncementAudienceScope.Department, TargetDepartmentId = null });

        result.ShouldHaveValidationErrorFor(command => command.TargetDepartmentId);
    }

    [Fact]
    public void Should_Have_Error_When_Location_Scoped_Without_A_Target_Location()
    {
        var result = _validator.TestValidate(
            CreateCommand() with { AudienceScope = AnnouncementAudienceScope.Location, TargetLocationId = null });

        result.ShouldHaveValidationErrorFor(command => command.TargetLocationId);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(CreateCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateAnnouncementCommand CreateCommand() => new(
        "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
        PublishAt, null, false, null);
}
