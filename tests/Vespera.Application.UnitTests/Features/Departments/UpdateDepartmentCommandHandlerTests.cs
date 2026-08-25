using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Departments;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Departments;

public class UpdateDepartmentCommandHandlerTests
{
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateDepartmentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateDepartmentCommandHandler CreateHandler() => new(_departments, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Rename_And_Reparent_When_Department_Exists()
    {
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "seed").Value;
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns(department);

        var handler = CreateHandler();
        var command = new UpdateDepartmentCommand(department.Id.Value, "Platform Engineering", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        department.Name.Should().Be("Platform Engineering");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Department_Missing()
    {
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns((Department?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateDepartmentCommand(Guid.NewGuid(), "Engineering", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("department.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Department_Would_Become_Its_Own_Parent()
    {
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "seed").Value;
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns(department);

        var handler = CreateHandler();
        var command = new UpdateDepartmentCommand(department.Id.Value, "Engineering", department.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("department.self_parent");
    }
}
