using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Departments;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Departments;

public class DeleteDepartmentCommandHandlerTests
{
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public DeleteDepartmentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private DeleteDepartmentCommandHandler CreateHandler() => new(_departments, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Soft_Delete_When_Department_Exists()
    {
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "seed").Value;
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns(department);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteDepartmentCommand(department.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        department.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Department_Missing()
    {
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns((Department?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteDepartmentCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("department.not_found");
    }
}
