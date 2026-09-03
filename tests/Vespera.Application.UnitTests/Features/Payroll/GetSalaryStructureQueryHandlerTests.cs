using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetSalaryStructureQueryHandlerTests
{
    private readonly IReadRepository<SalaryStructure> _structures = Substitute.For<IReadRepository<SalaryStructure>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetSalaryStructureQueryHandler CreateHandler() =>
        new(_structures, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    [Fact]
    public async Task Handle_Should_Return_The_Structure_As_A_Dto_And_Record_Pii_Access()
    {
        var now = DateTimeOffset.UtcNow;
        var employeeId = EmployeeId.New();
        var componentId = SalaryComponentId.New();
        var structure = SalaryStructure.Create(
            _tenantId, employeeId, Money.Of(50000m, Currency.Inr),
            [SalaryStructureLine.Of(componentId, SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr)))],
            new DateOnly(2026, 1, 1), null).Value;

        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _structures.FirstOrDefaultAsync(Arg.Any<SalaryStructureByEmployeeActiveOnDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns(structure);

        var result = await CreateHandler().Handle(new GetSalaryStructureQuery(employeeId.Value, new DateOnly(2026, 6, 1)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EmployeeId.Should().Be(employeeId.Value);
        result.Value.Lines.Should().ContainSingle(line => line.ComponentId == componentId.Value && line.FixedAmount == 50000m);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "SalaryStructure", structure.Id.Value, "Lines", "system", now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_No_Structure_Is_Active_On_The_Requested_Date()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _structures.FirstOrDefaultAsync(Arg.Any<SalaryStructureByEmployeeActiveOnDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns((SalaryStructure?)null);

        var result = await CreateHandler().Handle(
            new GetSalaryStructureQuery(Guid.NewGuid(), new DateOnly(2026, 6, 1)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        await _piiAccessAuditor.DidNotReceive().RecordAccessAsync(
            Arg.Any<TenantId>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }
}
