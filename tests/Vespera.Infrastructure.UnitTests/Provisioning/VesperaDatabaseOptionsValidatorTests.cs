using FluentAssertions;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.UnitTests.Provisioning;

public class VesperaDatabaseOptionsValidatorTests
{
    private readonly VesperaDatabaseOptionsValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_For_Default_Options()
    {
        var result = _validator.Validate(null, new VesperaDatabaseOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_External_Mode_Has_No_ConnectionStringName()
    {
        var options = new VesperaDatabaseOptions { Mode = DatabaseMode.External };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("External:ConnectionStringName");
    }

    [Fact]
    public void Validate_Should_Succeed_When_External_Mode_Has_A_ConnectionStringName()
    {
        var options = new VesperaDatabaseOptions
        {
            Mode = DatabaseMode.External,
            External = new ExternalDatabaseOptions { ConnectionStringName = "VesperaExternal" },
        };

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void Validate_Should_Fail_For_Out_Of_Range_Local_Port(int port)
    {
        var options = new VesperaDatabaseOptions { Local = new LocalDatabaseOptions { Host = "localhost", Port = port } };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Local:Port");
    }

    [Fact]
    public void Validate_Should_Fail_For_Zero_Startup_Timeout()
    {
        var options = new VesperaDatabaseOptions
        {
            Docker = new DockerDatabaseOptions { Image = "postgres", ContainerName = "c", VolumeName = "v", StartupTimeoutSeconds = 0 },
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("StartupTimeoutSeconds");
    }
}
