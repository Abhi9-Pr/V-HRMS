using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Eis.Events;
using OffboardingChecklist = Vespera.Domain.Assets.OffboardingChecklist;

namespace Vespera.Application.UnitTests.Features.Assets;

public class EmployeeExitedNotificationHandlerTests
{
    private readonly IReadRepositoryAdmin<SoftwareLicenseAllocation> _allocationReads = Substitute.For<IReadRepositoryAdmin<SoftwareLicenseAllocation>>();
    private readonly IReadRepositoryAdmin<SoftwareLicense> _licenses = Substitute.For<IReadRepositoryAdmin<SoftwareLicense>>();
    private readonly IReadRepositoryAdmin<AssetAssignment> _assignments = Substitute.For<IReadRepositoryAdmin<AssetAssignment>>();
    private readonly IWriteRepository<AssetRecovery> _recoveryWrites = Substitute.For<IWriteRepository<AssetRecovery>>();
    private readonly IReadRepositoryAdmin<OffboardingChecklist> _checklistReads = Substitute.For<IReadRepositoryAdmin<OffboardingChecklist>>();
    private readonly IWriteRepository<OffboardingChecklist> _checklistWrites = Substitute.For<IWriteRepository<OffboardingChecklist>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public EmployeeExitedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _checklistReads.ListIgnoringFiltersAsync(Arg.Any<OffboardingChecklistByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<OffboardingChecklist>());
    }

    private EmployeeExitedNotificationHandler CreateHandler() => new(
        _allocationReads, _licenses, _assignments, _recoveryWrites, _checklistReads, _checklistWrites, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Release_Every_Active_License_Allocation_And_Its_Seat()
    {
        var employeeId = EmployeeId.New();
        var license = SoftwareLicense.Create(_tenantId, "Figma", 5, null, DateTimeOffset.UtcNow, "system").Value;
        license.AssignSeat();
        var allocation = SoftwareLicenseAllocation.Allocate(_tenantId, license.Id, employeeId, DateTimeOffset.UtcNow);

        _allocationReads.ListIgnoringFiltersAsync(Arg.Any<ActiveSoftwareLicenseAllocationsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns([allocation]);
        _licenses.ListIgnoringFiltersAsync(Arg.Any<SoftwareLicenseByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([license]);
        _assignments.ListIgnoringFiltersAsync(Arg.Any<ActiveAssetAssignmentsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<AssetAssignment>());

        var handler = CreateHandler();
        await handler.Handle(
            new DomainEventNotification<EmployeeExited>(new EmployeeExited(employeeId, _tenantId, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow)),
            CancellationToken.None);

        allocation.IsActive.Should().BeFalse();
        license.SeatsUsed.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Initiate_Recovery_And_Create_A_Checklist_For_Every_Active_Assignment()
    {
        var employeeId = EmployeeId.New();
        var assignment = AssetAssignment.Assign(_tenantId, AssetId.New(), employeeId, DateTimeOffset.UtcNow);

        _allocationReads.ListIgnoringFiltersAsync(Arg.Any<ActiveSoftwareLicenseAllocationsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<SoftwareLicenseAllocation>());
        _assignments.ListIgnoringFiltersAsync(Arg.Any<ActiveAssetAssignmentsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns([assignment]);

        var handler = CreateHandler();
        await handler.Handle(
            new DomainEventNotification<EmployeeExited>(new EmployeeExited(employeeId, _tenantId, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow)),
            CancellationToken.None);

        await _recoveryWrites.Received(1).AddAsync(
            Arg.Is<AssetRecovery>(r => r.AssetAssignmentId == assignment.Id && r.EmployeeId == employeeId), Arg.Any<CancellationToken>());
        await _checklistWrites.Received(1).AddAsync(Arg.Any<OffboardingChecklist>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Not_Create_A_Duplicate_Checklist_When_One_Already_Exists()
    {
        var employeeId = EmployeeId.New();
        var assignment = AssetAssignment.Assign(_tenantId, AssetId.New(), employeeId, DateTimeOffset.UtcNow);
        var existingChecklist = OffboardingChecklist.Create(_tenantId, employeeId, ["Return badge"]);

        _allocationReads.ListIgnoringFiltersAsync(Arg.Any<ActiveSoftwareLicenseAllocationsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<SoftwareLicenseAllocation>());
        _assignments.ListIgnoringFiltersAsync(Arg.Any<ActiveAssetAssignmentsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _checklistReads.ListIgnoringFiltersAsync(Arg.Any<OffboardingChecklistByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns([existingChecklist]);

        var handler = CreateHandler();
        await handler.Handle(
            new DomainEventNotification<EmployeeExited>(new EmployeeExited(employeeId, _tenantId, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow)),
            CancellationToken.None);

        await _checklistWrites.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
