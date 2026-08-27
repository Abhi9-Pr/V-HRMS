using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Dashboard;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Dashboard;

public class SaveDashboardLayoutCommandHandlerTests
{
    private readonly IReadRepository<DashboardLayout> _layouts = Substitute.For<IReadRepository<DashboardLayout>>();
    private readonly IWriteRepository<DashboardLayout> _layoutWriter = Substitute.For<IWriteRepository<DashboardLayout>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDashboardWidgetProvider _knownProvider = Substitute.For<IDashboardWidgetProvider>();

    public SaveDashboardLayoutCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _knownProvider.WidgetKey.Returns("shiftTracker");
        _layouts.FirstOrDefaultAsync(Arg.Any<ISpecification<DashboardLayout>>(), Arg.Any<CancellationToken>())
            .Returns((DashboardLayout?)null);
    }

    private SaveDashboardLayoutCommandHandler CreateHandler() =>
        new(_layouts, _layoutWriter, [_knownProvider], _tenantContext, _currentUser);

    [Fact]
    public async Task Handle_Should_Create_A_New_Layout_When_None_Exists()
    {
        var handler = CreateHandler();
        var command = new SaveDashboardLayoutCommand([new WidgetPreferenceInput("shiftTracker", 0, true, "Medium")], null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _layoutWriter.Received(1).AddAsync(
            Arg.Is<DashboardLayout>(l => l.Widgets.Count == 1 && l.Widgets[0].WidgetKey == "shiftTracker"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Reject_A_Widget_Key_That_Is_Not_Registered()
    {
        var handler = CreateHandler();
        var command = new SaveDashboardLayoutCommand([new WidgetPreferenceInput("notARealWidget", 0, true, "Medium")], null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _layoutWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Update_An_Existing_Layout()
    {
        var existing = DashboardLayout.CreateDefault(_tenantContext.TenantId, new Domain.IdentityAccess.UserId(_currentUser.UserId!.Value),
            [("shiftTracker", 0, true, WidgetSize.Medium)]);
        _layouts.FirstOrDefaultAsync(Arg.Any<ISpecification<DashboardLayout>>(), Arg.Any<CancellationToken>()).Returns(existing);
        var handler = CreateHandler();
        var command = new SaveDashboardLayoutCommand([new WidgetPreferenceInput("shiftTracker", 5, false, "Large")], null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Widgets.Single().SortOrder.Should().Be(5);
        existing.Widgets.Single().IsVisible.Should().BeFalse();
        _layoutWriter.Received(1).Update(existing);
    }
}
