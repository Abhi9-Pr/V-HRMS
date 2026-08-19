using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Departments;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Departments;

public class CreateDepartmentCommandHandlerTests
{
    private readonly IWriteRepository<Department> _departments = Substitute.For<IWriteRepository<Department>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateDepartmentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateDepartmentCommandHandler CreateHandler() =>
        new(_departments, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_Department_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new CreateDepartmentCommand("Engineering", "ENG", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _departments.Received(1).AddAsync(
            Arg.Is<Department>(d => d.Name == "Engineering" && d.Code == "ENG"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Name_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateDepartmentCommand(" ", "ENG", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _departments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
