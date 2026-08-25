using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Security;

namespace Vespera.Infrastructure.UnitTests.Security;

/// <summary>
/// No real clamd daemon runs in this environment, so these tests stand up a bare
/// <see cref="TcpListener"/> that speaks just enough of the INSTREAM reply protocol to exercise
/// <see cref="ClamAvVirusScanner"/>'s parsing, via the internal TCP-client-factory test seam.
/// </summary>
public class ClamAvVirusScannerTests
{
    [Theory]
    [InlineData("stream: OK", ScanResult.Clean)]
    [InlineData("stream: Eicar-Test-Signature FOUND", ScanResult.Infected)]
    [InlineData("stream: garbled ERROR", ScanResult.ScanFailed)]
    public async Task ScanAsync_Should_Parse_Clamd_Reply(string reply, ScanResult expected)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var acceptTask = AcceptAndReplyAsync(listener, reply);

        var options = Options.Create(new VirusScanOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 5 });
        var scanner = new ClamAvVirusScanner(options, () => new TcpClient());

        using var content = new MemoryStream("test content"u8.ToArray());
        var result = await scanner.ScanAsync(content, CancellationToken.None);

        result.Should().Be(expected);
        await acceptTask;
    }

    /// <summary>
    /// "zINSTREAM\0" (10 bytes) + 4-byte big-endian length + the chunk itself + a 4-byte
    /// zero-length terminator is exactly what <see cref="ClamAvVirusScanner"/> sends for a
    /// single-chunk payload — read exactly that many bytes (known from the test's own content
    /// length) rather than guessing at "no more data", which is racy over loopback.
    /// </summary>
    private static async Task AcceptAndReplyAsync(TcpListener listener, string reply)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();

        const int expectedRequestBytes = 10 + 4 + 12 + 4; // "test content" is 12 bytes.
        var buffer = new byte[expectedRequestBytes];
        var totalRead = 0;
        while (totalRead < expectedRequestBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead));
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        await stream.WriteAsync(Encoding.ASCII.GetBytes(reply));
    }
}
