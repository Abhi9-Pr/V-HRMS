using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class EndReportingRelationshipCommandHandler : IRequestHandler<EndReportingRelationshipCommand, Result>
{
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;

    public EndReportingRelationshipCommandHandler(IReadRepository<ReportingRelationship> reportingRelationships)
    {
        _reportingRelationships = reportingRelationships;
    }

    public async Task<Result> Handle(EndReportingRelationshipCommand request, CancellationToken cancellationToken)
    {
        var specification = new ReportingRelationshipByIdSpecification(new ReportingRelationshipId(request.Id));
        var relationship = await _reportingRelationships.FirstOrDefaultAsync(specification, cancellationToken);
        if (relationship is null || relationship.EmployeeId != new EmployeeId(request.EmployeeId))
        {
            return Result.Failure(Error.NotFound("reporting_relationship.not_found", "Reporting relationship not found."));
        }

        return relationship.EndOn(request.ValidTo);
    }
}
