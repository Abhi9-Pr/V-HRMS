using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Recruitment;

public sealed class CreateCandidateCommandHandler : IRequestHandler<CreateCandidateCommand, Result<Guid>>
{
    private readonly IWriteRepository<Candidate> _candidates;
    private readonly ITenantContext _tenantContext;

    public CreateCandidateCommandHandler(IWriteRepository<Candidate> candidates, ITenantContext tenantContext)
    {
        _candidates = candidates;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var phoneResult = PhoneNumber.Create(request.Phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure<Guid>(phoneResult.Error);
        }

        var result = Candidate.Create(
            _tenantContext.TenantId, new JobRequisitionId(request.JobRequisitionId), request.FullName, emailResult.Value, phoneResult.Value);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _candidates.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
