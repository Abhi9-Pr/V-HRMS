using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Attendance;

public class QuarantinedBiometricPunchTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset PunchedAtUtc = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Start_Pending()
    {
        var entry = Create();

        entry.Status.Should().Be(QuarantinedBiometricPunchStatus.Pending);
        entry.ResolvedEmployeeId.Should().BeNull();
    }

    [Fact]
    public void Resolve_Should_Set_Status_And_ResolvedEmployeeId()
    {
        var entry = Create();
        var employeeId = EmployeeId.New();

        var result = entry.Resolve(employeeId);

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(QuarantinedBiometricPunchStatus.Resolved);
        entry.ResolvedEmployeeId.Should().Be(employeeId);
    }

    [Fact]
    public void Resolve_Should_Fail_When_Already_Resolved()
    {
        var entry = Create();
        entry.Resolve(EmployeeId.New());

        var result = entry.Resolve(EmployeeId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("quarantined_biometric_punch.already_resolved");
    }

    [Fact]
    public void Create_Should_Expose_Every_Field_Including_A_Null_PunchType()
    {
        var deviceId = BiometricDeviceId.New();

        var entry = QuarantinedBiometricPunch.Create(TenantId, deviceId, "ZK-099", PunchedAtUtc, null, "rec-2");

        entry.TenantId.Should().Be(TenantId);
        entry.BiometricDeviceId.Should().Be(deviceId);
        entry.DeviceUserId.Should().Be("ZK-099");
        entry.PunchedAtUtc.Should().Be(PunchedAtUtc);
        entry.PunchType.Should().BeNull();
        entry.ExternalRecordId.Should().Be("rec-2");
    }

    [Fact]
    public void New_Ids_Should_Be_Distinct()
    {
        QuarantinedBiometricPunchId.New().Should().NotBe(QuarantinedBiometricPunchId.New());
    }

    private static QuarantinedBiometricPunch Create() =>
        QuarantinedBiometricPunch.Create(TenantId, BiometricDeviceId.New(), "ZK-042", PunchedAtUtc, PunchType.In, "rec-1");
}
