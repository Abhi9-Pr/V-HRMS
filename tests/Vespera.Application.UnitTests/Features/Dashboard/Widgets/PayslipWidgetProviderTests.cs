using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class PayslipWidgetProviderTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private PayslipWidgetProvider CreateProvider() => new(_sender);

    [Fact]
    public void Should_Expose_Its_Widget_Metadata()
    {
        var provider = CreateProvider();

        provider.WidgetKey.Should().Be("payslip");
        provider.DefaultVisible.Should().BeTrue();
        provider.DefaultSize.Should().Be(WidgetSize.Small);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_Null_When_There_Is_No_Payslip_Yet()
    {
        _sender.Send(Arg.Any<GetMyLatestPayslipQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<MyLatestPayslipDto?>(null));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Propagate_The_Senders_Failure()
    {
        var error = Error.Failure("payslip.unavailable", "Could not load the latest payslip.");
        _sender.Send(Arg.Any<GetMyLatestPayslipQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<MyLatestPayslipDto?>(error));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
