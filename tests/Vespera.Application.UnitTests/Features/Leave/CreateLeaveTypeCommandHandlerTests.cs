using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateLeaveTypeCommandHandlerTests
{
    private readonly IWriteRepository<LeaveType> _writer = Substitute.For<IWriteRepository<LeaveType>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private CreateLeaveTypeCommandHandler CreateHandler() => new(_writer, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_A_LeaveType_And_Return_Its_Id()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.Email.Returns("hr@demo.vespera.test");

        var result = await CreateHandler().Handle(new CreateLeaveTypeCommand("Sick Leave", true, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _writer.Received(1).AddAsync(Arg.Is<LeaveType>(t => t.Name == "Sick Leave" && t.IsPaid), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Name_Is_Empty()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new CreateLeaveTypeCommand(string.Empty, true, 5), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.name_required");
        await _writer.DidNotReceive().AddAsync(Arg.Any<LeaveType>(), Arg.Any<CancellationToken>());
    }
}
