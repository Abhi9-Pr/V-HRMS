using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetUnusedSeatsReportQueryHandlerTests
{
    private readonly IReadRepository<SoftwareLicense> _licenses = Substitute.For<IReadRepository<SoftwareLicense>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetUnusedSeatsReportQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetUnusedSeatsReportQueryHandler CreateHandler() => new(_licenses, _tenantContext);

    [Fact]
    public async Task Handle_Should_Compute_Unused_Seats_Per_License()
    {
        var license = SoftwareLicense.Create(_tenantId, "Figma", 5, null, DateTimeOffset.UtcNow, "system").Value;
        license.AssignSeat();
        license.AssignSeat();

        _licenses.ListAsync(Arg.Any<AllSoftwareLicensesSpecification>(), Arg.Any<CancellationToken>()).Returns([license]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetUnusedSeatsReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(row => row.LicenseId == license.Id.Value && row.UnusedSeats == 3);
    }
}
