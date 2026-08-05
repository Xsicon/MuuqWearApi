using MuuqWear.Model.DTO.CustomerDTO;
using MuuqWear.Model.Models.Profiles;

namespace MuuqWear.Application.Shared;

/// <summary>
/// Shared suspension expiry + auth gate helpers (lazy clear on read).
/// </summary>
public static class AccountAccessGuard
{
    public static bool IsCurrentlySuspended(Profiles profile, DateTime? utcNow = null) =>
        AccountSuspension.IsCurrentlySuspended(
            profile.AccountStatus,
            profile.SuspendedUntil,
            utcNow);

    public static string ResolveEffectiveStatus(Profiles profile, DateTime? utcNow = null) =>
        AccountSuspension.ResolveEffectiveStatus(
            profile.IsDeleted,
            profile.AccountStatus,
            profile.SuspendedUntil,
            utcNow);

    /// <summary>
    /// If suspension has expired, clears flags in-memory and persists when possible.
    /// </summary>
    public static async Task ClearExpiredSuspensionAsync(
        Supabase.Client client,
        Profiles profile,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (!string.Equals(profile.AccountStatus, AccountStatusValues.Suspended, StringComparison.OrdinalIgnoreCase))
            return;

        if (!profile.SuspendedUntil.HasValue || profile.SuspendedUntil.Value > now)
            return;

        profile.AccountStatus = AccountStatusValues.Active;
        profile.SuspendedUntil = null;
        profile.SuspensionReason = null;
        profile.SuspendedByUserId = null;
        profile.SuspendedAt = null;
        profile.ReactivatedAt = now;
        profile.ReactivatedByUserId = null;

        if (profile.Id is not { } id || id == Guid.Empty)
            return;

        try
        {
            await client
                .From<Profiles>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(p => p.AccountStatus, AccountStatusValues.Active)
                .Set(p => p.SuspendedUntil!, null)
                .Set(p => p.SuspensionReason!, null)
                .Set(p => p.SuspendedByUserId!, null)
                .Set(p => p.SuspendedAt!, null)
                .Set(p => p.ReactivatedAt!, now)
                .Set(p => p.ReactivatedByUserId!, null)
                .Update();
        }
        catch
        {
            // Lazy clear is best-effort; in-memory state still treats account as active.
        }
    }

    public static async Task<Profiles?> LoadProfileAsync(Supabase.Client client, Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        var result = await client
            .From<Profiles>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
            .Limit(1)
            .Get();

        return result.Models.FirstOrDefault();
    }

    public static async Task<(bool Blocked, string? Message, string AccountStatus, DateTime? SuspendedUntil)>
        EvaluateAccessAsync(Supabase.Client client, Guid userId)
    {
        var profile = await LoadProfileAsync(client, userId);
        if (profile == null)
            return (false, null, AccountStatusValues.Active, null);

        await ClearExpiredSuspensionAsync(client, profile);

        if (profile.IsDeleted)
        {
            return (
                true,
                "This account has been deleted. Please contact support.",
                AccountStatusValues.Deleted,
                null);
        }

        if (IsCurrentlySuspended(profile))
        {
            var until = profile.SuspendedUntil!.Value;
            return (
                true,
                AccountSuspension.BuildSuspendedMessage(until),
                AccountStatusValues.Suspended,
                until);
        }

        return (false, null, AccountStatusValues.Active, null);
    }
}
