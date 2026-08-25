using System.Globalization;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;

namespace Vespera.Infrastructure.Attendance;

/// <summary>
/// ZKTeco's ADMS gateway exposes attendance-log rows as plain-text, pipe-delimited lines over
/// HTTP (the shape a real device/cloud gateway's <c>/iclock/getrequest</c>-style polling endpoint
/// returns): one punch per line, fields
/// <c>deviceUserId|punchedAtUtc (ISO-8601)|punchCode|externalRecordId</c>, where
/// <c>punchCode</c> is <c>"0"</c> for an In punch, <c>"1"</c> for an Out punch, and anything else
/// (including a blank field) means the device didn't report a distinguishable direction — the
/// caller infers it by toggling against the employee's last known punch, per
/// <see cref="BiometricPunchRecord.PunchType"/>'s own doc comment. This is a correct,
/// independently-testable fetch-and-parse implementation of the one shape this codebase's
/// deployments need, not a full ADMS-protocol SDK (push-webhook ingestion, device provisioning,
/// etc. are out of scope here) — see docs/adding-a-biometric-vendor.md for what a second vendor
/// adapter needs to implement.
/// </summary>
public sealed class ZkTecoBiometricDeviceAdapter : IBiometricDeviceAdapter
{
    private readonly HttpClient _httpClient;

    public ZkTecoBiometricDeviceAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BiometricFetchResult> FetchSinceAsync(
        BiometricDeviceConnection connection, string? cursor, CancellationToken cancellationToken)
    {
        var uri = new Uri($"http://{connection.Host}:{connection.Port}/iclock/getrequest?cursor={Uri.EscapeDataString(cursor ?? string.Empty)}");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var records = new List<BiometricPunchRecord>();
        string? nextCursor = null;

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = line.Split('|');
            if (fields.Length < 4)
            {
                continue;
            }

            var deviceUserId = fields[0];
            var punchedAtUtc = DateTimeOffset.Parse(fields[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            PunchType? punchType = fields[2] switch
            {
                "0" => PunchType.In,
                "1" => PunchType.Out,
                _ => null,
            };
            var externalRecordId = fields[3];

            records.Add(new BiometricPunchRecord(deviceUserId, punchedAtUtc, punchType, externalRecordId));
            nextCursor = externalRecordId;
        }

        return new BiometricFetchResult(records, nextCursor ?? cursor);
    }
}
