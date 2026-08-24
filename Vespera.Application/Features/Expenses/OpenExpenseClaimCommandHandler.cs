using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class OpenExpenseClaimCommandHandler : IRequestHandler<OpenExpenseClaimCommand, Result<Guid>>
{
    private readonly IWriteRepository<ExpenseClaim> _expenseClaims;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public OpenExpenseClaimCommandHandler(
        IWriteRepository<ExpenseClaim> expenseClaims, ITenantContext tenantContext, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _expenseClaims = expenseClaims;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<Guid>> Handle(OpenExpenseClaimCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure<Guid>(Error.Validation("expense_claim.no_employee", "The signed-in account is not linked to an employee."));
        }

        var claim = ExpenseClaim.Open(_tenantContext.TenantId, employeeId.Value, request.SettlementCurrency);
        await _expenseClaims.AddAsync(claim, cancellationToken);

        return Result.Success(claim.Id.Value);
    }
}
