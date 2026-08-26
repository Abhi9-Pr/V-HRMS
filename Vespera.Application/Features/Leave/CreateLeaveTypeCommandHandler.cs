using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class CreateLeaveTypeCommandHandler : IRequestHandler<CreateLeaveTypeCommand, Result<Guid>>
{
    private readonly IWriteRepository<LeaveType> _writer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateLeaveTypeCommandHandler(
        IWriteRepository<LeaveType> writer, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _writer = writer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var result = LeaveType.Create(
            _tenantContext.TenantId, request.Name, request.IsPaid, request.CarryForwardLimit, _dateTimeProvider.UtcNow,
            _currentUser.Email ?? "system");
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _writer.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
