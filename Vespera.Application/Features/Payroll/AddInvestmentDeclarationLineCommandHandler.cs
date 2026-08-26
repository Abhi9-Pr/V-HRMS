using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll;

public sealed class AddInvestmentDeclarationLineCommandHandler : IRequestHandler<AddInvestmentDeclarationLineCommand, Result<Guid>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<InvestmentDeclaration> _declarations;
    private readonly IWriteRepository<InvestmentDeclaration> _declarationWriter;
    private readonly IReadRepository<InvestmentDeclarationWindow> _windows;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddInvestmentDeclarationLineCommandHandler(
        IReadRepository<User> users, IReadRepository<InvestmentDeclaration> declarations,
        IWriteRepository<InvestmentDeclaration> declarationWriter, IReadRepository<InvestmentDeclarationWindow> windows,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _declarations = declarations;
        _declarationWriter = declarationWriter;
        _windows = windows;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddInvestmentDeclarationLineCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<Guid>(Error.Unauthorized("investment_declaration.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Failure<Guid>(Error.Validation("investment_declaration.no_employee_profile", "This user has no linked employee profile."));
        }

        var window = await _windows.FirstOrDefaultAsync(
            new InvestmentDeclarationWindowByTenantAndFySpecification(tenantId, request.FinancialYear), cancellationToken);
        if (window is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "investment_declaration.no_window", "No investment declaration window is configured for this financial year."));
        }

        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var declaration = await _declarations.FirstOrDefaultAsync(
            new InvestmentDeclarationByEmployeeAndFySpecification(tenantId, employeeId, request.FinancialYear), cancellationToken);
        var isNew = declaration is null;

        if (declaration is null)
        {
            var createResult = InvestmentDeclaration.Create(
                tenantId, employeeId, new TaxRegimeVersionId(request.TaxRegimeVersionId), request.FinancialYear, today, window);
            if (createResult.IsFailure)
            {
                return Result.Failure<Guid>(createResult.Error);
            }

            declaration = createResult.Value;
        }

        var addLineResult = declaration.AddLine(
            request.Section, Money.Of(request.Amount, Currency.Inr), request.ProofFileReference, today, window);
        if (addLineResult.IsFailure)
        {
            return Result.Failure<Guid>(addLineResult.Error);
        }

        if (isNew)
        {
            await _declarationWriter.AddAsync(declaration, cancellationToken);
        }
        else
        {
            _declarationWriter.Update(declaration);
        }

        return Result.Success(declaration.Id.Value);
    }
}
