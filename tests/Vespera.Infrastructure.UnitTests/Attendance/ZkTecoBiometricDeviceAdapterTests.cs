using System.Net;
using FluentAssertions;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Infrastructure.Attendance;

namespace Vespera.Infrastructure.UnitTests.Attendance;

public class ZkTecoBiometricDeviceAdapterTests
{
    private static readonly BiometricDeviceConnection Connection = new("device.local", 4370, null);

    [Fact]
    public async Task FetchSinceAsync_Should_Parse_Pipe_Delimited_Lines_Into_Records()
    {
        const string body = "ZK-001|2026-02-01T03:30:00Z|0|rec-1\nZK-002|2026-02-01T12:30:00Z|1|rec-2\n";
        using var httpClient = new HttpClient(new FakeHandler(body));
        var adapter = new ZkTecoBiometricDeviceAdapter(httpClient);

        var result = await adapter.FetchSinceAsync(Connection, null, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].DeviceUserId.Should().Be("ZK-001");
        result.Records[0].PunchType.Should().Be(PunchType.In);
        result.Records[1].PunchType.Should().Be(PunchType.Out);
        result.NextCursor.Should().Be("rec-2");
    }

    [Fact]
    public async Task FetchSinceAsync_Should_Leave_PunchType_Null_For_An_Undistinguishable_Code()
    {
        const string body = "ZK-003|2026-02-01T03:30:00Z|9|rec-3\n";
        using var httpClient = new HttpClient(new FakeHandler(body));
        var adapter = new ZkTecoBiometricDeviceAdapter(httpClient);

        var result = await adapter.FetchSinceAsync(Connection, null, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].PunchType.Should().BeNull();
    }

    [Fact]
    public async Task FetchSinceAsync_Should_Return_The_Given_Cursor_Unchanged_When_There_Are_No_New_Records()
    {
        using var httpClient = new HttpClient(new FakeHandler(string.Empty));
        var adapter = new ZkTecoBiometricDeviceAdapter(httpClient);

        var result = await adapter.FetchSinceAsync(Connection, "rec-1", CancellationToken.None);

        result.Records.Should().BeEmpty();
        result.NextCursor.Should().Be("rec-1");
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string _body;

        public FakeHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) });
    }
}
