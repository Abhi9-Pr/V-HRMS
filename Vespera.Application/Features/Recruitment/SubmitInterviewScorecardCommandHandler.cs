using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class SubmitInterviewScorecardCommandHandler : IRequestHandler<SubmitInterviewScorecardCommand, Result>
{
    private readonly IReadRepository<Interview> _interviewReads;
    private readonly IWriteRepository<Interview> _interviews;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SubmitInterviewScorecardCommandHandler(
        IReadRepository<Interview> interviewReads, IWriteRepository<Interview> interviews, IDateTimeProvider dateTimeProvider)
    {
        _interviewReads = interviewReads;
        _interviews = interviews;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SubmitInterviewScorecardCommand request, CancellationToken cancellationToken)
    {
        var interview = await _interviewReads.FirstOrDefaultAsync(
            new InterviewByIdSpecification(new InterviewId(request.InterviewId)), cancellationToken);

        if (interview is null)
        {
            return Result.Failure(Error.NotFound("interview.not_found", "Interview not found."));
        }

        var result = interview.SubmitScorecard(new EmployeeId(request.InterviewerId), request.Rating, request.Notes, _dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        _interviews.Update(interview);
        return Result.Success();
    }
}
