using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class CreateSalaryStructureCommandHandlerTests
{
    private readonly IReadRepository<SalaryStructure> _existingStructures = Substitute.For<IReadRepository<SalaryStructure>>();
    private readonly IWriteRepository<SalaryStructure> _structures = Substitute.For<IWriteRepository<SalaryStructure>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    private CreateSalaryStructureCommandHandler CreateHandler() => new(_existingStructures, _structures, _tenantContext);

    private static CreateSalaryStructureCommand ValidCommand(Guid employeeId, Guid componentId, DateOnly validFrom) =>
        new(employeeId, 50000m, [new SalaryStructureLineRequest(componentId, nameof(SalaryComponentFormulaKind.FixedAmount), 50000m, null, null, null)],
            validFrom, null);

    [Fact]
    public async Task Handle_Should_Create_A_Structure_With_A_FixedAmount_Line()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _existingStructures.ListAsync(Arg.Any<SalaryStructuresActiveOnDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = ValidCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _structures.Received(1).AddAsync(Arg.Any<SalaryStructure>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_A_Line_Formula_Is_Invalid_Or_Incomplete()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _existingStructures.ListAsync(Arg.Any<SalaryStructuresActiveOnDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = new CreateSalaryStructureCommand(
            Guid.NewGuid(), 50000m, [new SalaryStructureLineRequest(Guid.NewGuid(), nameof(SalaryComponentFormulaKind.FixedAmount), null, null, null, null)],
            new DateOnly(2026, 1, 1), null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("salary_structure.invalid_line");
        await _structures.DidNotReceive().AddAsync(Arg.Any<SalaryStructure>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_An_Active_Structure_Already_Overlaps_For_The_Same_Employee()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var employeeId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        var existing = SalaryStructure.Create(
            _tenantId, new EmployeeId(employeeId), Money.Of(40000m, Currency.Inr),
            [SalaryStructureLine.Of(new SalaryComponentId(componentId), SalaryComponentFormula.FixedAmount(Money.Of(40000m, Currency.Inr)))],
            new DateOnly(2026, 1, 1), null).Value;

        _existingStructures.ListAsync(Arg.Any<SalaryStructuresActiveOnDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns([existing]);

        var command = ValidCommand(employeeId, componentId, new DateOnly(2026, 6, 1));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("effective_dated.overlap");
        await _structures.DidNotReceive().AddAsync(Arg.Any<SalaryStructure>(), Arg.Any<CancellationToken>());
    }
}
