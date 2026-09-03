using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetSoftwareLicensesQueryHandlerTests
{
    private readonly IReadRepository<SoftwareLicense> _licenses = Substitute.For<IReadRepository<SoftwareLicense>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetSoftwareLicensesQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetSoftwareLicensesQueryHandler CreateHandler() => new(_licenses, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Software_Licenses()
    {
        var license = SoftwareLicense.Create(_tenantId, "Figma", 5, null, DateTimeOffset.UtcNow, "system").Value;
        license.AssignSeat();

        _licenses.ListAsync(Arg.Any<SoftwareLicensesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([license]);
        _licenses.CountAsync(Arg.Any<SoftwareLicensesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetSoftwareLicensesQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == license.Id.Value && dto.ProductName == "Figma" && dto.SeatsUsed == 1);
        result.Value.TotalCount.Should().Be(1);
    }
}
