using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class GetReportingRelationshipsQueryHandlerTests
{
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();

    private GetReportingRelationshipsQueryHandler CreateHandler() => new(_reportingRelationships);

    [Fact]
    public async Task Handle_Should_Return_The_Employees_Reporting_History()
    {
        var employeeId = EmployeeId.New();
        var relationship = ReportingRelationship.Create(TenantId.New(), employeeId, EmployeeId.New(), new DateOnly(2026, 1, 1), null).Value;
        _reportingRelationships.ListAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ReportingRelationship>)[relationship]);

        var result = await CreateHandler().Handle(new GetReportingRelationshipsQuery(employeeId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == relationship.Id.Value && dto.EmployeeId == employeeId.Value);
    }
}
