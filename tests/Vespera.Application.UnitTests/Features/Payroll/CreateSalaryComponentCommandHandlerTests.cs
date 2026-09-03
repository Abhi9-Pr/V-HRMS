using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class CreateSalaryComponentCommandHandlerTests
{
    private readonly IWriteRepository<SalaryComponent> _components = Substitute.For<IWriteRepository<SalaryComponent>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private CreateSalaryComponentCommandHandler CreateHandler() => new(_components, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Stage_A_New_Component_And_Return_Its_Id()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new CreateSalaryComponentCommand("Basic", nameof(SalaryComponentType.Earning), true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _components.Received(1).AddAsync(Arg.Is<SalaryComponent>(c => c.Name == "Basic" && c.ComponentType == SalaryComponentType.Earning), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_For_An_Unknown_Component_Type()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var result = await CreateHandler().Handle(new CreateSalaryComponentCommand("Basic", "NotARealType", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("salary_component.invalid_type");
        await _components.DidNotReceive().AddAsync(Arg.Any<SalaryComponent>(), Arg.Any<CancellationToken>());
    }
}
