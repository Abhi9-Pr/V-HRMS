using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Eis.Events;

namespace Vespera.Application.UnitTests.Features.Eis;

public class EmployeeExitedDomainEventHandlerTests
{
    private readonly IReadRepositoryAdmin<OffboardingChecklist> _checklistsAdmin = Substitute.For<IReadRepositoryAdmin<OffboardingChecklist>>();
    private readonly IWriteRepository<OffboardingChecklist> _checklistWriter = Substitute.For<IWriteRepository<OffboardingChecklist>>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private static readonly DateOnly ExitDate = new(2026, 1, 31);
    private static readonly DateTimeOffset OccurredOn = new(2026, 1, 31, 9, 0, 0, TimeSpan.Zero);

    private EmployeeExitedDomainEventHandler CreateHandler() => new(_checklistsAdmin, _checklistWriter);

    [Fact]
    public async Task Handle_Should_Create_A_Checklist_When_None_Exists()
    {
        _checklistsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<OffboardingChecklist>)[]);

        var notification = new DomainEventNotification<EmployeeExited>(
            new EmployeeExited(_employeeId, _tenantId, ExitDate, OccurredOn));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _checklistWriter.Received(1).AddAsync(
            Arg.Is<OffboardingChecklist>(c => c.EmployeeId == _employeeId && c.TenantId == _tenantId && c.ExitDate == ExitDate),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Be_Idempotent_When_A_Checklist_Already_Exists()
    {
        var existingChecklist = OffboardingChecklist.Initiate(_tenantId, _employeeId, ExitDate, OccurredOn, "system").Value;
        _checklistsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<OffboardingChecklist>)[existingChecklist]);

        var notification = new DomainEventNotification<EmployeeExited>(
            new EmployeeExited(_employeeId, _tenantId, ExitDate, OccurredOn));

        await CreateHandler().Handle(notification, CancellationToken.None);
        await CreateHandler().Handle(notification, CancellationToken.None);

        await _checklistWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
