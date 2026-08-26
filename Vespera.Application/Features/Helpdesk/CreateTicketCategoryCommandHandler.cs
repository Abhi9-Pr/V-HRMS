using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CreateTicketCategoryCommandHandler : IRequestHandler<CreateTicketCategoryCommand, Result<Guid>>
{
    private readonly IWriteRepository<TicketCategory> _categories;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTicketCategoryCommandHandler(
        IWriteRepository<TicketCategory> categories, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _categories = categories;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateTicketCategoryCommand request, CancellationToken cancellationToken)
    {
        var defaultSlaPolicyId = request.DefaultSlaPolicyId is { } id ? new SlaPolicyId(id) : (SlaPolicyId?)null;

        var result = TicketCategory.Create(
            _tenantContext.TenantId, request.Name, new DepartmentId(request.DepartmentId), defaultSlaPolicyId,
            _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _categories.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
