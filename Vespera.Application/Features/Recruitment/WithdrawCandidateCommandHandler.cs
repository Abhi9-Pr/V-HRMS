using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class WithdrawCandidateCommandHandler : IRequestHandler<WithdrawCandidateCommand, Result>
{
    private readonly IReadRepository<Candidate> _candidateReads;
    private readonly IWriteRepository<Candidate> _candidates;

    public WithdrawCandidateCommandHandler(IReadRepository<Candidate> candidateReads, IWriteRepository<Candidate> candidates)
    {
        _candidateReads = candidateReads;
        _candidates = candidates;
    }

    public async Task<Result> Handle(WithdrawCandidateCommand request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateReads.FirstOrDefaultAsync(
            new CandidateByIdSpecification(new CandidateId(request.CandidateId)), cancellationToken);

        if (candidate is null)
        {
            return Result.Failure(Error.NotFound("candidate.not_found", "Candidate not found."));
        }

        var result = candidate.Withdraw();
        if (result.IsFailure)
        {
            return result;
        }

        _candidates.Update(candidate);
        return Result.Success();
    }
}
