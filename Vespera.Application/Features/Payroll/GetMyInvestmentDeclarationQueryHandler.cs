using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Payroll;

public sealed class GetMyInvestmentDeclarationQueryHandler : IRequestHandler<GetMyInvestmentDeclarationQuery, Result<InvestmentDeclarationDto?>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<Domain.Payroll.InvestmentDeclaration> _declarations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetMyInvestmentDeclarationQueryHandler(
        IReadRepository<User> users, IReadRepository<Domain.Payroll.InvestmentDeclaration> declarations, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _users = users;
        _declarations = declarations;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<InvestmentDeclarationDto?>> Handle(GetMyInvestmentDeclarationQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<InvestmentDeclarationDto?>(Error.Unauthorized("investment_declaration.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Failure<InvestmentDeclarationDto?>(
                Error.Validation("investment_declaration.no_employee_profile", "This user has no linked employee profile."));
        }

        var declaration = await _declarations.FirstOrDefaultAsync(
            new InvestmentDeclarationByEmployeeAndFySpecification(tenantId, employeeId, request.FinancialYear), cancellationToken);

        if (declaration is null)
        {
            return Result.Success<InvestmentDeclarationDto?>(null);
        }

        await _piiAccessAuditor.RecordAccessAsync(
            tenantId, "InvestmentDeclaration", declaration.Id.Value, "Lines", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        var lines = declaration.Lines
            .Select((line, index) => new InvestmentDeclarationLineDto(
                index, line.Section, line.Amount.Amount, line.ProofFileReference, line.ReviewStatus.ToString(), line.ReviewComment))
            .ToList();

        return Result.Success<InvestmentDeclarationDto?>(
            new InvestmentDeclarationDto(declaration.Id.Value, declaration.FinancialYear, declaration.Status.ToString(), lines));
    }
}
