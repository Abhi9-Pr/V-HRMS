using FluentAssertions;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Security;

namespace Vespera.Infrastructure.UnitTests.Security;

public class NullVirusScannerTests
{
    [Fact]
    public async Task ScanAsync_Should_Always_Return_Clean()
    {
        var scanner = new NullVirusScanner();
        using var content = new MemoryStream("not actually a virus"u8.ToArray());

        var result = await scanner.ScanAsync(content, CancellationToken.None);

        result.Should().Be(ScanResult.Clean);
    }
}
