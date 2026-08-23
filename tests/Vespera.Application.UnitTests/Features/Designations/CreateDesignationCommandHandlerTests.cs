using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Designations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Designations;

public class CreateDesignationCommandHandlerTests
{
    private readonly IWriteRepository<Designation> _designations = Substitute.For<IWriteRepository<Designation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateDesignationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateDesignationCommandHandler CreateHandler() =>
        new(_designations, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_Designation_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new CreateDesignationCommand("Software Engineer", 3, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _designations.Received(1).AddAsync(
            Arg.Is<Designation>(d => d.Title == "Software Engineer" && d.Grade == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Title_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateDesignationCommand(" ", 3, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _designations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Grade_Is_Not_Positive()
    {
        var handler = CreateHandler();
        var command = new CreateDesignationCommand("Software Engineer", 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _designations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
