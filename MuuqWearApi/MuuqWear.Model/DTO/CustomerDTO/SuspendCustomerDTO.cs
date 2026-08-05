namespace MuuqWear.Model.DTO.CustomerDTO;

public class SuspendCustomerDTO
{
    /// <summary>Allowed: 1, 3, 7, 14, 30, 60, 90, 180, 365.</summary>
    public int DurationDays { get; set; }

    /// <summary>Optional internal admin reason (max 500 chars).</summary>
    public string? Reason { get; set; }
}

public class ReactivateCustomerDTO
{
    /// <summary>Optional audit note.</summary>
    public string? Reason { get; set; }
}

public class AccountAccessStatusDTO
{
    public bool IsActive { get; set; }
    public string AccountStatus { get; set; } = AccountStatusValues.Active;
    public DateTime? SuspendedUntil { get; set; }
}
