using FluentAssertions;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Eis;

public class ConsentRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Start_Granted()
    {
        var consent = CreateConsent();

        consent.Granted.Should().BeTrue();
        consent.GrantedAt.Should().Be(Now);
        consent.WithdrawnAt.Should().BeNull();
    }

    [Fact]
    public void Withdraw_Should_Clear_Granted_And_Set_WithdrawnAt()
    {
        var consent = CreateConsent();
        var withdrawnAt = Now.AddDays(1);

        var result = consent.Withdraw(withdrawnAt);

        result.IsSuccess.Should().BeTrue();
        consent.Granted.Should().BeFalse();
        consent.WithdrawnAt.Should().Be(withdrawnAt);
    }

    [Fact]
    public void Withdraw_Should_Fail_When_Already_Withdrawn()
    {
        var consent = CreateConsent();
        consent.Withdraw(Now);

        var result = consent.Withdraw(Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("consent_record.already_withdrawn");
    }

    [Fact]
    public void Grant_Should_Fail_When_Already_Granted()
    {
        var consent = CreateConsent();

        var result = consent.Grant();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("consent_record.already_granted");
    }

    [Fact]
    public void Grant_Should_Reinstate_A_Withdrawn_Consent()
    {
        var consent = CreateConsent();
        consent.Withdraw(Now);

        var result = consent.Grant();

        result.IsSuccess.Should().BeTrue();
        consent.Granted.Should().BeTrue();
        consent.WithdrawnAt.Should().BeNull();
    }

    [Fact]
    public void ConsentRecordId_Instances_With_The_Same_Value_Should_Be_Equal()
    {
        var value = Guid.NewGuid();

        new ConsentRecordId(value).Should().Be(new ConsentRecordId(value));
        ConsentRecordId.New().Should().NotBe(ConsentRecordId.New());
    }

    private static ConsentRecord CreateConsent() =>
        new(ConsentRecordId.New(), ConsentType.DataProcessing, Now);
}
