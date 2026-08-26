using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetTicketByIdQueryValidator : AbstractValidator<GetTicketByIdQuery>
{
    public GetTicketByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
