using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class CompleteOffboardingChecklistItemCommandHandler : IRequestHandler<CompleteOffboardingChecklistItemCommand, Result>
{
    private readonly IReadRepository<OffboardingChecklist> _checklists;

    public CompleteOffboardingChecklistItemCommandHandler(IReadRepository<OffboardingChecklist> checklists)
    {
        _checklists = checklists;
    }

    public async Task<Result> Handle(CompleteOffboardingChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var checklist = await _checklists.FirstOrDefaultAsync(
            new OffboardingChecklistByIdSpecification(new OffboardingChecklistId(request.ChecklistId)), cancellationToken);

        if (checklist is null)
        {
            return Result.Failure(Error.NotFound("offboarding_checklist.not_found", "Offboarding checklist not found."));
        }

        return checklist.CompleteItem(request.ItemIndex);
    }
}
