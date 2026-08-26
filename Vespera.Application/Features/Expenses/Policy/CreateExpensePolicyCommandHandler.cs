using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class CreateExpensePolicyCommandHandler : IRequestHandler<CreateExpensePolicyCommand, Result<Guid>>
{
    private readonly IWriteRepository<ExpensePolicy> _expensePolicies;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateExpensePolicyCommandHandler(
        IWriteRepository<ExpensePolicy> expensePolicies, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _expensePolicies = expensePolicies;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateExpensePolicyCommand request, CancellationToken cancellationToken)
    {
        var result = ExpensePolicy.Create(
            _tenantContext.TenantId,
            request.Category,
            Money.Of(request.MaxAmountPerClaim, request.Currency),
            Money.Of(request.ReceiptRequiredAboveAmount, request.Currency),
            _dateTimeProvider.UtcNow,
            _currentUser.UserId?.ToString() ?? "system",
            request.ApplicableDesignationId is { } designationId ? new DesignationId(designationId) : null,
            request.MaxAmountSeverity,
            request.ReceiptRequiredSeverity);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _expensePolicies.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
