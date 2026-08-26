using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class SubmitInvestmentDeclarationCommandHandler : IRequestHandler<SubmitInvestmentDeclarationCommand, Result>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<InvestmentDeclaration> _declarations;
    private readonly IReadRepository<InvestmentDeclarationWindow> _windows;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SubmitInvestmentDeclarationCommandHandler(
        IReadRepository<User> users, IReadRepository<InvestmentDeclaration> declarations,
        IReadRepository<InvestmentDeclarationWindow> windows, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _declarations = declarations;
        _windows = windows;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SubmitInvestmentDeclarationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("investment_declaration.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Failure(Error.Validation("investment_declaration.no_employee_profile", "This user has no linked employee profile."));
        }

        var declaration = await _declarations.FirstOrDefaultAsync(
            new InvestmentDeclarationByEmployeeAndFySpecification(tenantId, employeeId, request.FinancialYear), cancellationToken);
        if (declaration is null)
        {
            return Result.Failure(Error.NotFound("investment_declaration.not_found", "No declaration found for this financial year."));
        }

        var window = await _windows.FirstOrDefaultAsync(
            new InvestmentDeclarationWindowByTenantAndFySpecification(tenantId, request.FinancialYear), cancellationToken);
        if (window is null)
        {
            return Result.Failure(Error.NotFound(
                "investment_declaration.no_window", "No investment declaration window is configured for this financial year."));
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        return declaration.Submit(today, window);
    }
}
