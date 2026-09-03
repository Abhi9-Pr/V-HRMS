using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Designations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Designations;

public class GetDesignationsETagQueryHandlerTests
{
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetDesignationsETagQueryHandler CreateHandler() => new(_designations, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_The_Same_Hash_For_The_Same_Designations()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var designation = Designation.Create(_tenantId, "Software Engineer", 5, DateTimeOffset.UtcNow, "system").Value;
        _designations.ListAsync(Arg.Any<DesignationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([designation]);

        var first = await CreateHandler().Handle(new GetDesignationsETagQuery(), CancellationToken.None);
        var second = await CreateHandler().Handle(new GetDesignationsETagQuery(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.Should().NotBeNullOrEmpty();
        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Different_Hash_When_The_Designation_Set_Differs()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var designationA = Designation.Create(_tenantId, "Software Engineer", 5, DateTimeOffset.UtcNow, "system").Value;
        var designationB = Designation.Create(_tenantId, "Product Manager", 6, DateTimeOffset.UtcNow, "system").Value;

        _designations.ListAsync(Arg.Any<DesignationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([designationA]);
        var first = await CreateHandler().Handle(new GetDesignationsETagQuery(), CancellationToken.None);

        _designations.ListAsync(Arg.Any<DesignationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([designationA, designationB]);
        var second = await CreateHandler().Handle(new GetDesignationsETagQuery(), CancellationToken.None);

        second.Value.Should().NotBe(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Hash_Even_When_There_Are_No_Designations()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _designations.ListAsync(Arg.Any<DesignationsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetDesignationsETagQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }
}
