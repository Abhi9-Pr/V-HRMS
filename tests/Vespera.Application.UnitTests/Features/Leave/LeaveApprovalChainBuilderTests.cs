using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class LeaveApprovalChainBuilderTests
{
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<Role> _roles = Substitute.For<IReadRepository<Role>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private LeaveApprovalChainBuilder CreateBuilder() => new(_reportingRelationships, _roles, _users);

    private static LeavePolicy CreatePolicy(bool requiresSkipLevel, decimal? skipLevelThreshold, bool requiresHr)
    {
        var policy = LeavePolicy.Create(TenantId.New(), LeaveTypeId.New(), 12, 1, 5, new DateOnly(2025, 1, 1), null).Value;
        policy.ConfigureApprovalChain(requiresSkipLevel, skipLevelThreshold, requiresHr);
        return policy;
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Return_Only_The_Direct_Manager_By_Default()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var relationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(relationship);
        var policy = CreatePolicy(requiresSkipLevel: false, skipLevelThreshold: null, requiresHr: false);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(managerId);
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Fail_When_No_Active_Manager()
    {
        var employeeId = EmployeeId.New();
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns((ReportingRelationship?)null);
        var policy = CreatePolicy(requiresSkipLevel: false, skipLevelThreshold: null, requiresHr: false);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.no_manager");
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Add_SkipLevel_When_Policy_Requires_It()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var skipLevelManagerId = EmployeeId.New();
        var employeeRelationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        var managerRelationship = ReportingRelationship.Create(_tenantId, managerId, skipLevelManagerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(
                Arg.Is<ActiveManagerRelationshipSpecification>(_ => true), Arg.Any<CancellationToken>())
            .Returns(employeeRelationship, managerRelationship);
        var policy = CreatePolicy(requiresSkipLevel: true, skipLevelThreshold: null, requiresHr: false);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(managerId, skipLevelManagerId);
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Add_SkipLevel_When_RequestedDays_Exceeds_Threshold()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var skipLevelManagerId = EmployeeId.New();
        var employeeRelationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        var managerRelationship = ReportingRelationship.Create(_tenantId, managerId, skipLevelManagerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employeeRelationship, managerRelationship);
        var policy = CreatePolicy(requiresSkipLevel: false, skipLevelThreshold: 3m, requiresHr: false);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 5, Today, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(managerId, skipLevelManagerId);
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Not_Add_SkipLevel_When_No_SkipLevel_Manager_Exists()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var employeeRelationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employeeRelationship, (ReportingRelationship?)null);
        var policy = CreatePolicy(requiresSkipLevel: true, skipLevelThreshold: null, requiresHr: false);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(managerId);
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Add_HrApprover_When_Policy_Requires_It()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var hrEmployeeId = EmployeeId.New();
        var employeeRelationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employeeRelationship);
        var hrRole = Role.Create(_tenantId, "HR", Now, "system").Value;
        _roles.FirstOrDefaultAsync(Arg.Any<RoleByTenantAndNameSpecification>(), Arg.Any<CancellationToken>()).Returns(hrRole);
        var hrUserEmail = EmailAddress.Create("hr@vespera.test").Value;
        var hrUser = User.Create(_tenantId, hrUserEmail, hrEmployeeId, Now, "system");
        hrUser.AssignRole(hrRole.Id, Now, "system");
        _users.ListAsync(Arg.Any<UsersByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<User> { hrUser });
        var policy = CreatePolicy(requiresSkipLevel: false, skipLevelThreshold: null, requiresHr: true);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(managerId, hrEmployeeId);
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Fail_When_HR_Required_But_No_HR_Role_Configured()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var employeeRelationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employeeRelationship);
        _roles.FirstOrDefaultAsync(Arg.Any<RoleByTenantAndNameSpecification>(), Arg.Any<CancellationToken>()).Returns((Role?)null);
        var policy = CreatePolicy(requiresSkipLevel: false, skipLevelThreshold: null, requiresHr: true);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.no_hr_approver");
    }

    [Fact]
    public async Task BuildApproverSequenceAsync_Should_Fail_When_HR_Required_But_No_HR_User_Configured()
    {
        var employeeId = EmployeeId.New();
        var managerId = EmployeeId.New();
        var employeeRelationship = ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2025, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employeeRelationship);
        var hrRole = Role.Create(_tenantId, "HR", Now, "system").Value;
        _roles.FirstOrDefaultAsync(Arg.Any<RoleByTenantAndNameSpecification>(), Arg.Any<CancellationToken>()).Returns(hrRole);
        _users.ListAsync(Arg.Any<UsersByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<User>());
        var policy = CreatePolicy(requiresSkipLevel: false, skipLevelThreshold: null, requiresHr: true);

        var result = await CreateBuilder().BuildApproverSequenceAsync(_tenantId, employeeId, policy, 2, Today, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.no_hr_approver");
    }
}
