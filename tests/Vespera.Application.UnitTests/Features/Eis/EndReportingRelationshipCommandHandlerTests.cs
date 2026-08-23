using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class EndReportingRelationshipCommandHandlerTests
{
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();

    private EndReportingRelationshipCommandHandler CreateHandler() => new(_reportingRelationships);

    [Fact]
    public async Task Handle_Should_End_The_Relationship_When_Found()
    {
        var employeeId = EmployeeId.New();
        var relationship = ReportingRelationship.Create(TenantId.New(), employeeId, EmployeeId.New(), new DateOnly(2026, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(relationship);

        var result = await CreateHandler().Handle(
            new EndReportingRelationshipCommand(employeeId.Value, relationship.Id.Value, new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        relationship.ValidTo.Should().Be(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Relationship_Missing()
    {
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns((ReportingRelationship?)null);

        var result = await CreateHandler().Handle(
            new EndReportingRelationshipCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("reporting_relationship.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_EmployeeId_Does_Not_Match()
    {
        var relationship = ReportingRelationship.Create(TenantId.New(), EmployeeId.New(), EmployeeId.New(), new DateOnly(2026, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(relationship);

        var result = await CreateHandler().Handle(
            new EndReportingRelationshipCommand(EmployeeId.New().Value, relationship.Id.Value, new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("reporting_relationship.not_found");
    }
}
