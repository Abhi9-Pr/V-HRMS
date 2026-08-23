using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Security;

/// <summary>
/// Speaks clamd's <c>INSTREAM</c> protocol directly over TCP (no client NuGet needed — the wire
/// format is a handful of length-prefixed chunks). Selected via
/// <c>VirusScanOptions.Provider = "ClamAv"</c>; requires a reachable clamd daemon, which this
/// repo's dev/test environment does not run — see <see cref="NullVirusScanner"/> for local dev.
/// </summary>
public sealed class ClamAvVirusScanner : IVirusScanner
{
    private const int ChunkSize = 8192;
    private readonly VirusScanOptions _options;
    private readonly Func<TcpClient> _tcpClientFactory;

    public ClamAvVirusScanner(IOptions<VirusScanOptions> options)
        : this(options, () => new TcpClient())
    {
    }

    /// <summary>Test seam — lets a test substitute the TCP transport without a real socket.</summary>
    public ClamAvVirusScanner(IOptions<VirusScanOptions> options, Func<TcpClient> tcpClientFactory)
    {
        _options = options.Value;
        _tcpClientFactory = tcpClientFactory;
    }

    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken)
    {
        try
        {
            using var client = _tcpClientFactory();
            client.ReceiveTimeout = _options.TimeoutSeconds * 1000;
            client.SendTimeout = _options.TimeoutSeconds * 1000;

            await client.ConnectAsync(_options.Host, _options.Port, cancellationToken);
            await using var stream = client.GetStream();

            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);

            var buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                var lengthPrefix = BitConverter.GetBytes(bytesRead);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthPrefix);
                }

                await stream.WriteAsync(lengthPrefix, cancellationToken);
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            // Zero-length chunk terminates the stream per the INSTREAM protocol.
            await stream.WriteAsync(new byte[4], cancellationToken);

            var responseBuffer = new byte[4096];
            var responseLength = await stream.ReadAsync(responseBuffer, cancellationToken);
            var response = Encoding.ASCII.GetString(responseBuffer, 0, responseLength);

            if (response.Contains("FOUND", StringComparison.Ordinal))
            {
                return ScanResult.Infected;
            }

            if (response.Contains("OK", StringComparison.Ordinal))
            {
                return ScanResult.Clean;
            }

            return ScanResult.ScanFailed;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested is false)
        {
            return ScanResult.ScanFailed;
        }
    }
}
