using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class CancelInterviewCommandHandler : IRequestHandler<CancelInterviewCommand, Result>
{
    private readonly IReadRepository<Interview> _interviewReads;
    private readonly IWriteRepository<Interview> _interviews;

    public CancelInterviewCommandHandler(IReadRepository<Interview> interviewReads, IWriteRepository<Interview> interviews)
    {
        _interviewReads = interviewReads;
        _interviews = interviews;
    }

    public async Task<Result> Handle(CancelInterviewCommand request, CancellationToken cancellationToken)
    {
        var interview = await _interviewReads.FirstOrDefaultAsync(
            new InterviewByIdSpecification(new InterviewId(request.InterviewId)), cancellationToken);

        if (interview is null)
        {
            return Result.Failure(Error.NotFound("interview.not_found", "Interview not found."));
        }

        var result = interview.Cancel();
        if (result.IsFailure)
        {
            return result;
        }

        _interviews.Update(interview);
        return Result.Success();
    }
}
