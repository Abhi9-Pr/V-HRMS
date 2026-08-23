using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class GetReportingRelationshipsQueryHandler
    : IRequestHandler<GetReportingRelationshipsQuery, Result<IReadOnlyList<ReportingRelationshipDto>>>
{
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;

    public GetReportingRelationshipsQueryHandler(IReadRepository<ReportingRelationship> reportingRelationships)
    {
        _reportingRelationships = reportingRelationships;
    }

    public async Task<Result<IReadOnlyList<ReportingRelationshipDto>>> Handle(
        GetReportingRelationshipsQuery request, CancellationToken cancellationToken)
    {
        var specification = new ReportingRelationshipsByEmployeeSpecification(new EmployeeId(request.EmployeeId));
        var relationships = await _reportingRelationships.ListAsync(specification, cancellationToken);
        IReadOnlyList<ReportingRelationshipDto> dtos = relationships.Adapt<List<ReportingRelationshipDto>>();
        return Result.Success(dtos);
    }
}
