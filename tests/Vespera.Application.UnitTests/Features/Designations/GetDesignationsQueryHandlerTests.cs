using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Designations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Designations;

public class GetDesignationsQueryHandlerTests
{
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var designation = Designation.Create(tenantId, "Software Engineer", 3, DateTimeOffset.UtcNow, "system").Value;

        _designations.ListAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Designation> { designation });
        _designations.CountAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new GetDesignationsQueryHandler(_designations, _tenantContext);
        var query = new GetDesignationsQuery(new PagedRequest());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto =>
            dto.Title == "Software Engineer" && dto.Grade == 3 && dto.Id == designation.Id.Value);
    }
}
