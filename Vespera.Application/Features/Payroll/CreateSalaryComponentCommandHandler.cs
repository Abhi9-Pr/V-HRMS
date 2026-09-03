using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class CreateSalaryComponentCommandHandler : IRequestHandler<CreateSalaryComponentCommand, Result<Guid>>
{
    private readonly IWriteRepository<SalaryComponent> _components;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSalaryComponentCommandHandler(
        IWriteRepository<SalaryComponent> components, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _components = components;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSalaryComponentCommand request, CancellationToken cancellationToken)
    {
        // Validator already confirmed this parses — re-parsing here (not passing the enum
        // through the command itself) keeps the command's own shape a plain string, matching
        // every other string-enum request field elsewhere in this feature (e.g.
        // SalaryStructureLineRequest.FormulaKind).
        if (!Enum.TryParse<SalaryComponentType>(request.ComponentType, out var componentType))
        {
            return Result.Failure<Guid>(Error.Validation(
                "salary_component.invalid_type", $"'{request.ComponentType}' is not a valid component type."));
        }

        var createdBy = _currentUser.UserId?.ToString() ?? "system";
        var createResult = SalaryComponent.Create(
            _tenantContext.TenantId, request.Name, componentType, request.IsTaxable, _dateTimeProvider.UtcNow, createdBy);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _components.AddAsync(createResult.Value, cancellationToken);

        return Result.Success(createResult.Value.Id.Value);
    }
}
