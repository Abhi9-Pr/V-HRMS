using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Employees;

public class CreateEmployeeCommandHandlerTests
{
    private readonly IWriteRepository<Employee> _employees = Substitute.For<IWriteRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public CreateEmployeeCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateEmployeeCommandHandler CreateHandler() => new(_employees, _tenantContext, _currentUser, _dateTimeProvider);

    private static CreateEmployeeCommand ValidCommand() => new(
        "EMP-900", "Grace", "Hopper", "grace.hopper@vespera.test", "+14155552672",
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

    [Fact]
    public async Task Handle_Should_Add_Employee_And_Return_Its_Id_When_Valid()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _employees.Received(1).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Code_Is_Invalid()
    {
        var handler = CreateHandler();
        var command = ValidCommand() with { Code = "" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_code.empty");
        await _employees.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Email_Is_Invalid()
    {
        var handler = CreateHandler();
        var command = ValidCommand() with { WorkEmail = "not-an-email" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("email_address.invalid_format");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Phone_Is_Invalid()
    {
        var handler = CreateHandler();
        var command = ValidCommand() with { Phone = "not-a-phone" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("phone_number.invalid_format");
    }
}
