using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Eis;

public class DesignationTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_And_Trim_Title()
    {
        var result = Designation.Create(TenantId, "  Software Engineer  ", 5, Now, "system");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Software Engineer");
        result.Value.Grade.Should().Be(5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Title_Is_Missing(string title)
    {
        var result = Designation.Create(TenantId, title, 5, Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("designation.title_required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Fail_When_Grade_Is_Not_Positive(int grade)
    {
        var result = Designation.Create(TenantId, "Software Engineer", grade, Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("designation.invalid_grade");
    }

    [Fact]
    public void Rename_Should_Update_Title_And_Trim_It()
    {
        var designation = Designation.Create(TenantId, "Software Engineer", 5, Now, "system").Value;

        var result = designation.Rename("  Senior Software Engineer  ", Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        designation.Title.Should().Be("Senior Software Engineer");
    }

    [Fact]
    public void Rename_Should_Fail_When_Title_Is_Missing()
    {
        var designation = Designation.Create(TenantId, "Software Engineer", 5, Now, "system").Value;

        var result = designation.Rename(string.Empty, Now.AddDays(1), "admin");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("designation.title_required");
    }

    [Fact]
    public void ChangeGrade_Should_Update_Grade()
    {
        var designation = Designation.Create(TenantId, "Software Engineer", 5, Now, "system").Value;

        var result = designation.ChangeGrade(7, Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        designation.Grade.Should().Be(7);
    }

    [Fact]
    public void ChangeGrade_Should_Fail_When_Grade_Is_Not_Positive()
    {
        var designation = Designation.Create(TenantId, "Software Engineer", 5, Now, "system").Value;

        var result = designation.ChangeGrade(0, Now.AddDays(1), "admin");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("designation.invalid_grade");
    }
}
