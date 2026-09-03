using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CreateJobRequisitionCommandHandlerTests
{
    private readonly IWriteRepository<JobRequisition> _requisitions = Substitute.For<IWriteRepository<JobRequisition>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private CreateJobRequisitionCommandHandler CreateHandler() => new(_requisitions, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_And_Persist_A_Requisition()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(
            new CreateJobRequisitionCommand("Senior Engineer", Guid.NewGuid(), 2, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _requisitions.Received(1).AddAsync(Arg.Is<JobRequisition>(r => r.Title == "Senior Engineer"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Openings_Count_Is_Not_Positive()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(
            new CreateJobRequisitionCommand("Senior Engineer", Guid.NewGuid(), 0, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _requisitions.DidNotReceive().AddAsync(Arg.Any<JobRequisition>(), Arg.Any<CancellationToken>());
    }
}
