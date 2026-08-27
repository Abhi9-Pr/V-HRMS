using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Dashboard;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard;

public class GetDashboardQueryHandlerTests
{
    private readonly IReadRepository<DashboardLayout> _layouts = Substitute.For<IReadRepository<DashboardLayout>>();
    private readonly IDashboardWidgetCache _cache = Substitute.For<IDashboardWidgetCache>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public GetDashboardQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _layouts.FirstOrDefaultAsync(Arg.Any<ISpecification<DashboardLayout>>(), Arg.Any<CancellationToken>())
            .Returns((DashboardLayout?)null);
    }

    private static IDashboardWidgetProvider CreateProvider(
        string key, int order = 0, bool visible = true, WidgetSize size = WidgetSize.Medium, Func<Task<Result<object?>>>? payload = null)
    {
        var provider = Substitute.For<IDashboardWidgetProvider>();
        provider.WidgetKey.Returns(key);
        provider.DefaultOrder.Returns(order);
        provider.DefaultVisible.Returns(visible);
        provider.DefaultSize.Returns(size);
        provider.GetPayloadAsync(Arg.Any<CancellationToken>())
            .Returns(_ => payload is not null ? payload() : Task.FromResult(Result.Success<object?>(new { ok = true })));
        return provider;
    }

    [Fact]
    public async Task Handle_Should_Return_A_Successful_Envelope_For_Every_Visible_Widget()
    {
        var providers = new[] { CreateProvider("a", order: 0), CreateProvider("b", order: 1) };
        var handler = new GetDashboardQueryHandler(_layouts, providers, _cache, _tenantContext, _currentUser);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Widgets.Should().HaveCount(2);
        result.Value.Widgets.Should().OnlyContain(w => w.Success);
    }

    [Fact]
    public async Task Handle_Should_Isolate_A_Widget_That_Throws_Without_Failing_The_Others()
    {
        var brokenProvider = CreateProvider("broken", order: 0, payload: () => throw new InvalidOperationException("boom"));
        var healthyProvider = CreateProvider("healthy", order: 1);
        var handler = new GetDashboardQueryHandler(_layouts, [brokenProvider, healthyProvider], _cache, _tenantContext, _currentUser);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("one broken widget must not blank the whole dashboard");
        var broken = result.Value.Widgets.Single(w => w.WidgetKey == "broken");
        broken.Success.Should().BeFalse();
        broken.ErrorMessage.Should().NotBeNullOrEmpty();

        var healthy = result.Value.Widgets.Single(w => w.WidgetKey == "healthy");
        healthy.Success.Should().BeTrue();
        healthy.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Isolate_A_Widget_That_Returns_A_Failure_Result()
    {
        var failingProvider = CreateProvider(
            "failing", payload: () => Task.FromResult(Result.Failure<object?>(Error.Failure("widget.failed", "nope"))));
        var healthyProvider = CreateProvider("healthy", order: 1);
        var handler = new GetDashboardQueryHandler(_layouts, [failingProvider, healthyProvider], _cache, _tenantContext, _currentUser);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.Value.Widgets.Single(w => w.WidgetKey == "failing").Success.Should().BeFalse();
        result.Value.Widgets.Single(w => w.WidgetKey == "healthy").Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Exclude_Hidden_Widgets_From_Widgets_But_Keep_Them_In_Layout()
    {
        var hiddenProvider = CreateProvider("hidden", visible: false);
        var visibleProvider = CreateProvider("visible");
        var handler = new GetDashboardQueryHandler(_layouts, [hiddenProvider, visibleProvider], _cache, _tenantContext, _currentUser);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.Value.Widgets.Should().ContainSingle(w => w.WidgetKey == "visible");
        result.Value.Widgets.Should().NotContain(w => w.WidgetKey == "hidden");
        result.Value.Layout.Should().Contain(l => l.WidgetKey == "hidden" && !l.IsVisible);
    }
}
