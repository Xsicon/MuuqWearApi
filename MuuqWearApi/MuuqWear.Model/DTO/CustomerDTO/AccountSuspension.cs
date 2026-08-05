namespace MuuqWear.Model.DTO.CustomerDTO;

public static class AccountStatusValues
{
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Deleted = "deleted";
}

/// <summary>
/// Suspension duration helpers. Re-suspend always recalculates from UtcNow (not stacked).
/// </summary>
public static class AccountSuspension
{
    public static readonly int[] AllowedDurationDays =
        [1, 3, 7, 14, 30, 60, 90, 180, 365];

    public const int MaxReasonLength = 500;

    public static bool IsAllowedDuration(int durationDays) =>
        AllowedDurationDays.Contains(durationDays);

    public static DateTime ComputeSuspendedUntil(int durationDays, DateTime? utcNow = null) =>
        (utcNow ?? DateTime.UtcNow).AddDays(durationDays);

    public static bool IsCurrentlySuspended(
        string? accountStatus,
        DateTime? suspendedUntil,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return string.Equals(accountStatus, AccountStatusValues.Suspended, StringComparison.OrdinalIgnoreCase)
               && suspendedUntil.HasValue
               && suspendedUntil.Value > now;
    }

    public static string ResolveEffectiveStatus(
        bool isDeleted,
        string? accountStatus,
        DateTime? suspendedUntil,
        DateTime? utcNow = null)
    {
        if (isDeleted)
            return AccountStatusValues.Deleted;

        return IsCurrentlySuspended(accountStatus, suspendedUntil, utcNow)
            ? AccountStatusValues.Suspended
            : AccountStatusValues.Active;
    }

    public static string BuildSuspendedMessage(DateTime suspendedUntil) =>
        $"Your account is suspended until {suspendedUntil:yyyy-MM-ddTHH:mm:ssZ}.";
}
