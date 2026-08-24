using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class RescheduleInterviewCommandHandler : IRequestHandler<RescheduleInterviewCommand, Result>
{
    private readonly IReadRepository<Interview> _interviewReads;
    private readonly IWriteRepository<Interview> _interviews;

    public RescheduleInterviewCommandHandler(IReadRepository<Interview> interviewReads, IWriteRepository<Interview> interviews)
    {
        _interviewReads = interviewReads;
        _interviews = interviews;
    }

    public async Task<Result> Handle(RescheduleInterviewCommand request, CancellationToken cancellationToken)
    {
        var interview = await _interviewReads.FirstOrDefaultAsync(
            new InterviewByIdSpecification(new InterviewId(request.InterviewId)), cancellationToken);

        if (interview is null)
        {
            return Result.Failure(Error.NotFound("interview.not_found", "Interview not found."));
        }

        var result = interview.Reschedule(request.NewScheduledAt);
        if (result.IsFailure)
        {
            return result;
        }

        _interviews.Update(interview);
        return Result.Success();
    }
}
