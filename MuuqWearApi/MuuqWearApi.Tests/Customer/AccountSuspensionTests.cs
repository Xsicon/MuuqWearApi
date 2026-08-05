using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.CustomerDTO;
using MuuqWear.Model.Models.Profiles;
using Xunit;

namespace MuuqWearApi.Tests.Customer;

public class AccountSuspensionTests
{
    public static readonly object[][] AllowedDurations =
    [
        [1], [3], [7], [14], [30], [60], [90], [180], [365]
    ];

    [Theory]
    [MemberData(nameof(AllowedDurations))]
    public void ComputeSuspendedUntil_AddsExactDaysFromUtcNow(int days)
    {
        var now = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        var until = AccountSuspension.ComputeSuspendedUntil(days, now);
        Assert.Equal(now.AddDays(days), until);
        Assert.True(AccountSuspension.IsAllowedDuration(days));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(45)]
    [InlineData(400)]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsAllowedDuration_RejectsInvalidDays(int days) =>
        Assert.False(AccountSuspension.IsAllowedDuration(days));

    [Fact]
    public void IsCurrentlySuspended_TrueWhenActiveSuspension()
    {
        var now = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(AccountSuspension.IsCurrentlySuspended(
            AccountStatusValues.Suspended,
            now.AddDays(1),
            now));
    }

    [Fact]
    public void IsCurrentlySuspended_FalseWhenExpired()
    {
        var now = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(AccountSuspension.IsCurrentlySuspended(
            AccountStatusValues.Suspended,
            now.AddMinutes(-1),
            now));
    }

    [Fact]
    public void ResolveEffectiveStatus_PrefersDeleted()
    {
        var status = AccountSuspension.ResolveEffectiveStatus(
            isDeleted: true,
            accountStatus: AccountStatusValues.Suspended,
            suspendedUntil: DateTime.UtcNow.AddDays(3));

        Assert.Equal(AccountStatusValues.Deleted, status);
    }

    [Fact]
    public void ResolveEffectiveStatus_TreatsExpiredAsActive()
    {
        var now = DateTime.UtcNow;
        var status = AccountSuspension.ResolveEffectiveStatus(
            isDeleted: false,
            accountStatus: AccountStatusValues.Suspended,
            suspendedUntil: now.AddDays(-1),
            utcNow: now);

        Assert.Equal(AccountStatusValues.Active, status);
    }

    [Fact]
    public void BuildSuspendedMessage_IncludesIsoTimestamp()
    {
        var until = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);
        var message = AccountSuspension.BuildSuspendedMessage(until);
        Assert.Contains("2026-09-05T12:00:00Z", message);
        Assert.StartsWith("Your account is suspended until", message);
    }

    [Fact]
    public void CustomerDto_DefaultsAccountStatusActive()
    {
        var dto = new CustomerDTO();
        Assert.Equal(AccountStatusValues.Active, dto.AccountStatus);
    }

    [Fact]
    public void Profiles_IsCurrentlySuspended_MatchesHelper()
    {
        var profile = new Profiles
        {
            AccountStatus = AccountStatusValues.Suspended,
            SuspendedUntil = DateTime.UtcNow.AddDays(7)
        };

        Assert.True(AccountAccessGuard.IsCurrentlySuspended(profile));

        profile.SuspendedUntil = DateTime.UtcNow.AddMinutes(-5);
        Assert.False(AccountAccessGuard.IsCurrentlySuspended(profile));
    }
}
