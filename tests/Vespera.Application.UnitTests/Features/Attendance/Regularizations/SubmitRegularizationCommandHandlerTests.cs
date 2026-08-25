using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class SubmitRegularizationCommandHandlerTests
{
    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IWriteRepository<RegularizationRequest> _requestWriter = Substitute.For<IWriteRepository<RegularizationRequest>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private readonly Guid _userId = Guid.NewGuid();

    public SubmitRegularizationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(_userId);
    }

    private SubmitRegularizationCommandHandler CreateHandler() =>
        new(_attendanceDays, _users, _requestWriter, _tenantContext, _currentUser, _fileStorage);

    private AttendanceDay CreateOpenDay() => AttendanceDay.Open(_tenantId, _employeeId, new DateOnly(2026, 3, 10));

    [Fact]
    public async Task Handle_Should_Succeed_When_Caller_Is_The_Attendance_Days_Own_Employee()
    {
        var day = CreateOpenDay();
        var user = User.Create(_tenantId, EmailAddress.Create("a@vespera.test").Value, _employeeId, DateTimeOffset.UtcNow, "seed");
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns(day);
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(user);

        var command = new SubmitRegularizationCommand(day.Id.Value, "Forgot to punch out", null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _requestWriter.Received(1).AddAsync(Arg.Any<RegularizationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Is_Not_The_Attendance_Days_Own_Employee()
    {
        var day = CreateOpenDay();
        var otherUser = User.Create(_tenantId, EmailAddress.Create("b@vespera.test").Value, EmployeeId.New(), DateTimeOffset.UtcNow, "seed");
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns(day);
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(otherUser);

        var command = new SubmitRegularizationCommand(day.Id.Value, "Forgot to punch out", null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.self_only");
        await _requestWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Upload_Evidence_When_Provided()
    {
        var day = CreateOpenDay();
        var user = User.Create(_tenantId, EmailAddress.Create("a@vespera.test").Value, _employeeId, DateTimeOffset.UtcNow, "seed");
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns(day);
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("storage/key-evidence");

        var command = new SubmitRegularizationCommand(day.Id.Value, "Forgot to punch out", "note.pdf", [1, 2, 3]);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).UploadAsync("note.pdf", Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Attendance_Day_Does_Not_Exist()
    {
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns((AttendanceDay?)null);

        var command = new SubmitRegularizationCommand(Guid.NewGuid(), "Forgot to punch out", null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("attendance_day.not_found");
    }
}
