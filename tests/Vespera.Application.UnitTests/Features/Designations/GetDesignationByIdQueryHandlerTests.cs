using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Designations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Designations;

public class GetDesignationByIdQueryHandlerTests
{
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetDesignationByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetDesignationByIdQueryHandler CreateHandler() => new(_designations, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Dto_When_Designation_Exists()
    {
        var designation = Designation.Create(_tenantId, "Software Engineer", 3, DateTimeOffset.UtcNow, "seed").Value;
        _designations.FirstOrDefaultAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>()).Returns(designation);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDesignationByIdQuery(designation.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Software Engineer");
        result.Value.Grade.Should().Be(3);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Designation_Missing()
    {
        _designations.FirstOrDefaultAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>()).Returns((Designation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDesignationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("designation.not_found");
    }
}
