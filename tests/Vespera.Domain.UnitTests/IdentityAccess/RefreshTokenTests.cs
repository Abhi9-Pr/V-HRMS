using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.IdentityAccess.Events;

namespace Vespera.Domain.UnitTests.IdentityAccess;

public class RefreshTokenTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly UserId UserId = UserId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_Should_Assign_A_New_FamilyId_When_None_Is_Given()
    {
        var token = RefreshToken.Issue(TenantId, UserId, "hash", "device-1", Now, Now.AddDays(7)).Value;

        token.FamilyId.Should().NotBe(Guid.Empty);
        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void Rotate_Should_Revoke_The_Old_Token_And_Issue_A_New_One_In_The_Same_Family()
    {
        var original = RefreshToken.Issue(TenantId, UserId, "hash-1", "device-1", Now, Now.AddDays(7)).Value;

        var result = original.Rotate("hash-2", Now, Now.AddDays(14));

        result.IsSuccess.Should().BeTrue();
        original.RevokedAt.Should().Be(Now);
        original.ReplacedByTokenId.Should().Be(result.Value.Id);
        result.Value.FamilyId.Should().Be(original.FamilyId);
        result.Value.DeviceId.Should().Be(original.DeviceId);
    }

    [Fact]
    public void Rotate_Should_Fail_When_The_Token_Is_Already_Revoked()
    {
        var original = RefreshToken.Issue(TenantId, UserId, "hash-1", "device-1", Now, Now.AddDays(7)).Value;
        original.Rotate("hash-2", Now, Now.AddDays(14));

        var result = original.Rotate("hash-3", Now, Now.AddDays(14));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkReuseDetected_Should_Raise_RefreshTokenReuseDetected()
    {
        var token = RefreshToken.Issue(TenantId, UserId, "hash-1", "device-1", Now, Now.AddDays(7)).Value;

        token.MarkReuseDetected(Now);

        token.DomainEvents.Should().ContainSingle(e => e is RefreshTokenReuseDetected);
    }
}
