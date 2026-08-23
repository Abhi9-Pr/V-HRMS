# Adding a biometric device vendor

`Vespera.Application/Abstractions/Services/IBiometricDeviceAdapter.cs` is the one port every
vendor integration implements. `Vespera.Infrastructure/Attendance/ZkTecoBiometricDeviceAdapter.cs`
is the reference implementation — copy its shape. A second vendor needs exactly three things:

1. **Implement `FetchSinceAsync`'s cursor semantics for that vendor's API.** `cursor` is opaque —
   the poller (`BiometricPunchPollerHostedService`) never inspects or derives it, only persists
   whatever `BiometricFetchResult.NextCursor` your adapter returns and passes it back verbatim on
   the next call. Whatever "resume point" your vendor's API actually uses (a record id, a
   timestamp, a page token) is fine — that's entirely between your adapter and the vendor.

2. **Map that vendor's raw punch format to `BiometricPunchRecord`.** `DeviceUserId` is the raw id
   as the device knows it (`Employee.BiometricDeviceUserId` is what maps it to a real employee —
   your adapter never needs to know about `Employee` at all). `PunchType` is nullable — set it only
   if your vendor's API reports a distinguishable in/out code; leave it `null` if it doesn't, and
   the poller infers direction by toggling against the employee's last known punch, the same way
   the `Web`/`Mobile` punch paths already handle a client that doesn't send one. `ExternalRecordId`
   is the vendor's own record identifier — it's the poller's dedup key, so it must be stable and
   unique per punch across repeated fetches of the same window.

3. **Add one DI registration line.** With only one vendor today,
   `BiometricServiceCollectionExtensions.AddVesperaBiometricDevices` registers
   `ZkTecoBiometricDeviceAdapter` directly. Once a second vendor exists, switch that one method to
   the same `configuration["Provider"]`-driven selection `AddVesperaOcr`/`AddVesperaVirusScanning`
   already use (read one of those for the exact shape) — `BiometricDevice.VendorType` already
   carries per-device vendor identity for this, so the switch is keyed off each device's own
   `VendorType`, not one process-wide setting like OCR/virus-scan's provider selection is.

Nothing else changes. `BiometricDevice`, `QuarantinedBiometricPunch`,
`BiometricPunchPollerHostedService`, and the `BiometricDevicesController` are all vendor-agnostic —
none of them reference `ZkTecoBiometricDeviceAdapter` or any vendor-specific type.
