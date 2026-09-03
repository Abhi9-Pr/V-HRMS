using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetTicketCategoriesQueryHandlerTests
{
    private readonly IReadRepository<TicketCategory> _categories = Substitute.For<IReadRepository<TicketCategory>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var category = TicketCategory.Create(tenantId, "Hardware", DepartmentId.New(), null, DateTimeOffset.UtcNow, "system").Value;

        _categories.ListAsync(Arg.Any<ISpecification<TicketCategory>>(), Arg.Any<CancellationToken>()).Returns([category]);
        _categories.CountAsync(Arg.Any<ISpecification<TicketCategory>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetTicketCategoriesQueryHandler(_categories, _tenantContext);
        var result = await handler.Handle(new GetTicketCategoriesQuery(new PagedRequest()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Hardware" && dto.Id == category.Id.Value);
    }
}
