using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class CreateTicketCategoryCommandHandlerTests
{
    private readonly IWriteRepository<TicketCategory> _categories = Substitute.For<IWriteRepository<TicketCategory>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateTicketCategoryCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateTicketCategoryCommandHandler CreateHandler() => new(_categories, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Stage_A_New_Category_And_Return_Its_Id()
    {
        var command = new CreateTicketCategoryCommand("Hardware", Guid.NewGuid(), null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _categories.Received(1).AddAsync(Arg.Is<TicketCategory>(c => c.Name == "Hardware"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Set_The_Default_Sla_Policy_When_Given()
    {
        var slaPolicyId = Guid.NewGuid();
        var command = new CreateTicketCategoryCommand("Hardware", Guid.NewGuid(), slaPolicyId, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _categories.Received(1).AddAsync(
            Arg.Is<TicketCategory>(c => c.DefaultSlaPolicyId != null && c.DefaultSlaPolicyId.Value.Value == slaPolicyId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Name_Is_Blank()
    {
        var command = new CreateTicketCategoryCommand("   ", Guid.NewGuid(), null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket_category.name_required");
        await _categories.DidNotReceive().AddAsync(Arg.Any<TicketCategory>(), Arg.Any<CancellationToken>());
    }
}
