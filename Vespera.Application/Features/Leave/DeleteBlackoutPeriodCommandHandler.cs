using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class DeleteBlackoutPeriodCommandHandler : IRequestHandler<DeleteBlackoutPeriodCommand, Result>
{
    private readonly IReadRepository<BlackoutPeriod> _blackouts;
    private readonly IWriteRepository<BlackoutPeriod> _writer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBlackoutPeriodCommandHandler(
        IReadRepository<BlackoutPeriod> blackouts, IWriteRepository<BlackoutPeriod> writer, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _blackouts = blackouts;
        _writer = writer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBlackoutPeriodCommand request, CancellationToken cancellationToken)
    {
        var blackout = await _blackouts.FirstOrDefaultAsync(
            new BlackoutPeriodByIdSpecification(_tenantContext.TenantId, new BlackoutPeriodId(request.Id)), cancellationToken);
        if (blackout is null)
        {
            return Result.Failure(Error.NotFound("blackout_period.not_found", "Blackout period not found."));
        }

        var result = blackout.Delete(_dateTimeProvider.UtcNow, _currentUser.Email ?? "system");
        if (result.IsFailure)
        {
            return result;
        }

        _writer.Update(blackout);
        return Result.Success();
    }
}
