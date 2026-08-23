using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>Reviews one line, then automatically verifies the whole declaration if that was the
/// last line still <see cref="InvestmentDeclarationLineReviewStatus.Pending"/> — finance never has
/// to remember a separate "verify" step once every line has a decision.</summary>
public sealed class ReviewInvestmentDeclarationLineCommandHandler : IRequestHandler<ReviewInvestmentDeclarationLineCommand, Result>
{
    private readonly IReadRepository<InvestmentDeclaration> _declarations;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReviewInvestmentDeclarationLineCommandHandler(
        IReadRepository<InvestmentDeclaration> declarations, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _declarations = declarations;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReviewInvestmentDeclarationLineCommand request, CancellationToken cancellationToken)
    {
        var declaration = await _declarations.FirstOrDefaultAsync(
            new InvestmentDeclarationByIdSpecification(new InvestmentDeclarationId(request.DeclarationId)), cancellationToken);
        if (declaration is null)
        {
            return Result.Failure(Error.NotFound("investment_declaration.not_found", "Declaration not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var reviewResult = declaration.ReviewLine(
            request.LineIndex, request.Approved, request.Comment, _currentUser.UserId?.ToString() ?? "system", now);
        if (reviewResult.IsFailure)
        {
            return reviewResult;
        }

        if (declaration.Lines.All(line => line.ReviewStatus != InvestmentDeclarationLineReviewStatus.Pending))
        {
            var verifyResult = declaration.Verify();
            if (verifyResult.IsFailure)
            {
                return verifyResult;
            }
        }

        return Result.Success();
    }
}
