using System.Text;
using FluentAssertions;
using Vespera.Infrastructure.Ocr;

namespace Vespera.Infrastructure.UnitTests.Ocr;

public class HeuristicDocumentOcrServiceTests
{
    private readonly HeuristicDocumentOcrService _service = new();

    [Fact]
    public async Task ExtractAsync_Should_Extract_Vendor_Date_Amount_And_Tax_From_Receipt_Text()
    {
        var receiptText = "Uber India\nRide on 2026-01-10\nGST 18.50\nTotal 154.30";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(receiptText));

        var result = await _service.ExtractAsync(stream, CancellationToken.None);

        result.Fields["vendor"].Should().Be("Uber India");
        result.Fields["date"].Should().Be("2026-01-10");
        result.Fields["amount"].Should().Be("154.30");
        result.Fields["tax"].Should().Be("18.50");
        result.Confidence.Should().Be(1.0);
    }

    [Fact]
    public async Task ExtractAsync_Should_Return_Low_Confidence_When_Nothing_Matches()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

        var result = await _service.ExtractAsync(stream, CancellationToken.None);

        result.Fields.Should().BeEmpty();
        result.Confidence.Should().Be(0.0);
    }
}
