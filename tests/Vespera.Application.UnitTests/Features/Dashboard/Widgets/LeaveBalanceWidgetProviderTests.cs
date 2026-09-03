using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class LeaveBalanceWidgetProviderTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<LeaveBalance> _balances = Substitute.For<IReadRepository<LeaveBalance>>();
    private readonly IReadRepository<LeaveType> _leaveTypes = Substitute.For<IReadRepository<LeaveType>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public LeaveBalanceWidgetProviderTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private LeaveBalanceWidgetProvider CreateProvider() =>
        new(_balances, _leaveTypes, _tenantContext, new CurrentEmployeeResolver(_users, _currentUser));

    [Fact]
    public async Task GetPayloadAsync_Should_Return_An_Empty_Widget_When_There_Is_No_Signed_In_Employee()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().BeOfType<LeaveBalanceWidgetDto>().Subject;
        dto.Balances.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_Balances_Joined_To_Leave_Type_Names_Sorted_By_Name()
    {
        var employeeId = EmployeeId.New();
        var userId = Guid.NewGuid();
        var user = User.Create(TenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, Now, "system");
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);

        var sickLeaveType = LeaveType.Create(TenantId, "Sick Leave", true, 0, Now, "hr").Value;
        var annualLeaveType = LeaveType.Create(TenantId, "Annual Leave", true, 0, Now, "hr").Value;
        var orphanLeaveType = LeaveTypeId.New();

        var sickBalance = LeaveBalance.Open(TenantId, employeeId, sickLeaveType.Id);
        sickBalance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 5m, "Accrual", Now, "system");
        var annualBalance = LeaveBalance.Open(TenantId, employeeId, annualLeaveType.Id);
        annualBalance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Accrual", Now, "system");
        var orphanBalance = LeaveBalance.Open(TenantId, employeeId, orphanLeaveType);

        _balances.ListAsync(Arg.Any<LeaveBalancesByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns([sickBalance, annualBalance, orphanBalance]);
        _leaveTypes.ListAsync(Arg.Any<LeaveTypesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns([sickLeaveType, annualLeaveType]);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().BeOfType<LeaveBalanceWidgetDto>().Subject;
        dto.Balances.Should().HaveCount(2);
        dto.Balances.Select(b => b.LeaveTypeName).Should().ContainInOrder("Annual Leave", "Sick Leave");
        dto.Balances.Single(b => b.LeaveTypeName == "Annual Leave").Available.Should().Be(10m);
    }
}
