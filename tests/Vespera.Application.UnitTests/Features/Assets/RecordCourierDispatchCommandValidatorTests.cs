using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordCourierDispatchCommandValidatorTests
{
    private readonly RecordCourierDispatchCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RecoveryId_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordCourierDispatchCommand(Guid.Empty, "BlueDart", "TRK-1", null));

        result.ShouldHaveValidationErrorFor(c => c.RecoveryId);
    }

    [Fact]
    public void Should_Have_Error_When_Carrier_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordCourierDispatchCommand(Guid.NewGuid(), string.Empty, "TRK-1", null));

        result.ShouldHaveValidationErrorFor(c => c.Carrier);
    }

    [Fact]
    public void Should_Have_Error_When_Carrier_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new RecordCourierDispatchCommand(Guid.NewGuid(), new string('a', 129), "TRK-1", null));

        result.ShouldHaveValidationErrorFor(c => c.Carrier);
    }

    [Fact]
    public void Should_Have_Error_When_TrackingReference_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordCourierDispatchCommand(Guid.NewGuid(), "BlueDart", string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.TrackingReference);
    }

    [Fact]
    public void Should_Have_Error_When_TrackingReference_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new RecordCourierDispatchCommand(Guid.NewGuid(), "BlueDart", new string('a', 129), null));

        result.ShouldHaveValidationErrorFor(c => c.TrackingReference);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new RecordCourierDispatchCommand(Guid.NewGuid(), "BlueDart", "TRK-1", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
