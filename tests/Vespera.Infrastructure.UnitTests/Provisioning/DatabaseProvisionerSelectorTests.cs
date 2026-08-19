using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning;

namespace Vespera.Infrastructure.UnitTests.Provisioning;

public class DatabaseProvisionerSelectorTests
{
    [Fact]
    public async Task SelectAsync_Should_Prefer_External_Over_Local_And_Docker()
    {
        var external = ReadyCandidate("External", DatabaseStrategyKind.External);
        var local = ReadyCandidate("Local", DatabaseStrategyKind.Local);
        var docker = ReadyCandidate("Docker", DatabaseStrategyKind.Docker);

        var selector = CreateSelector([external, local, docker], "Development");

        var selected = await selector.SelectAsync(CancellationToken.None);

        selected.Should().Be(external.Provisioner);
    }

    [Fact]
    public async Task SelectAsync_Should_Prefer_Local_Over_Docker_When_External_Absent()
    {
        var external = NotReadyCandidate("External", DatabaseStrategyKind.External);
        var local = ReadyCandidate("Local", DatabaseStrategyKind.Local);
        var docker = ReadyCandidate("Docker", DatabaseStrategyKind.Docker);

        var selector = CreateSelector([external, local, docker], "Development");

        var selected = await selector.SelectAsync(CancellationToken.None);

        selected.Should().Be(local.Provisioner);
    }

    [Fact]
    public async Task SelectAsync_Should_Use_Docker_When_External_And_Local_Absent()
    {
        var external = NotReadyCandidate("External", DatabaseStrategyKind.External);
        var local = NotReadyCandidate("Local", DatabaseStrategyKind.Local);
        var docker = ReadyCandidate("Docker", DatabaseStrategyKind.Docker);

        var selector = CreateSelector([external, local, docker], "Development");

        var selected = await selector.SelectAsync(CancellationToken.None);

        selected.Should().Be(docker.Provisioner);
    }

    [Fact]
    public async Task SelectAsync_Should_Fall_Back_To_Sqlite_When_Everything_Else_Absent()
    {
        var external = NotReadyCandidate("External", DatabaseStrategyKind.External);
        var local = NotReadyCandidate("Local", DatabaseStrategyKind.Local);
        var docker = NotReadyCandidate("Docker", DatabaseStrategyKind.Docker);
        var sqlite = ReadyCandidate("SqliteFallback", DatabaseStrategyKind.SqliteFallback);

        var selector = CreateSelector([external, local, docker, sqlite], "Development");

        var selected = await selector.SelectAsync(CancellationToken.None);

        selected.Should().Be(sqlite.Provisioner);
    }

    [Fact]
    public async Task SelectAsync_Should_Throw_When_Nothing_Matches()
    {
        var external = NotReadyCandidate("External", DatabaseStrategyKind.External);
        var selector = CreateSelector([external], "Development");

        var act = () => selector.SelectAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SelectAsync_Should_Throw_In_Production_When_A_Non_External_Candidate_Is_Ready()
    {
        var local = ReadyCandidate("Local", DatabaseStrategyKind.Local);
        var selector = CreateSelector([local], "Production");

        var act = () => selector.SelectAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SelectAsync_Should_Allow_External_In_Production()
    {
        var external = ReadyCandidate("External", DatabaseStrategyKind.External);
        var selector = CreateSelector([external], "Production");

        var selected = await selector.SelectAsync(CancellationToken.None);

        selected.Should().Be(external.Provisioner);
    }

    /// <summary>
    /// OCP proof: a brand-new strategy — a fake candidate DatabaseProvisionerSelector has never
    /// seen — is picked up purely by being present in the registered candidate list. No line of
    /// DatabaseProvisionerSelector changes to make this pass.
    /// </summary>
    [Fact]
    public async Task SelectAsync_Should_Pick_Up_A_Newly_Registered_Candidate_With_No_Selector_Changes()
    {
        var external = NotReadyCandidate("External", DatabaseStrategyKind.External);
        var local = NotReadyCandidate("Local", DatabaseStrategyKind.Local);
        var docker = NotReadyCandidate("Docker", DatabaseStrategyKind.Docker);
        var brandNewStrategy = ReadyCandidate("SomeFutureCloudProvider", DatabaseStrategyKind.SqliteFallback);

        var selector = CreateSelector([external, local, docker, brandNewStrategy], "Development");

        var selected = await selector.SelectAsync(CancellationToken.None);

        selected.Should().Be(brandNewStrategy.Provisioner);
    }

    private static DatabaseProvisionerSelector CreateSelector(IEnumerable<DatabaseProvisioningCandidate> candidates, string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        return new DatabaseProvisionerSelector(candidates, environment, NullLogger<DatabaseProvisionerSelector>.Instance);
    }

    private static DatabaseProvisioningCandidate ReadyCandidate(string name, DatabaseStrategyKind kind) =>
        BuildCandidate(name, kind, DatabaseEnvironmentStatus.Ready);

    private static DatabaseProvisioningCandidate NotReadyCandidate(string name, DatabaseStrategyKind kind) =>
        BuildCandidate(name, kind, DatabaseEnvironmentStatus.NotProvisioned);

    private static DatabaseProvisioningCandidate BuildCandidate(string name, DatabaseStrategyKind kind, DatabaseEnvironmentStatus status)
    {
        var probe = Substitute.For<IDatabaseEnvironmentProbe>();
        probe.ProbeAsync(Arg.Any<CancellationToken>()).Returns(new DatabaseEnvironmentProbeResult(status, $"{name} probe: {status}"));

        var provisioner = Substitute.For<IDatabaseProvisioner>();

        return new DatabaseProvisioningCandidate(name, kind, probe, provisioner);
    }
}
