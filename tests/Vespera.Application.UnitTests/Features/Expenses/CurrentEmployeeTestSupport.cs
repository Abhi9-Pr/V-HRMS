using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

/// <summary>Builds a real (non-mocked) <see cref="CurrentEmployeeResolver"/> whose two injected
/// ports are stubbed to resolve to <paramref name="employeeId"/> — used by every handler test in
/// this feature that needs "the signed-in user" to resolve to a specific employee.</summary>
internal static class CurrentEmployeeTestSupport
{
    public static CurrentEmployeeResolver CreateResolver(TenantId tenantId, EmployeeId employeeId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)Guid.NewGuid());

        var users = Substitute.For<IReadRepository<User>>();
        var user = User.Create(tenantId, EmailAddress.Create("employee@demo.test").Value, employeeId, DateTimeOffset.UtcNow, "system");
        users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);

        return new CurrentEmployeeResolver(users, currentUser);
    }
}
