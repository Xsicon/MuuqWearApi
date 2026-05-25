namespace MuuqWear.Model.DTO.PartnerStoreProductDTO;

public class AffiliatePurchaseLimitDTO
{
    /// <summary>
    /// Number of items purchased in current month
    /// </summary>
    public int ItemsPurchasedThisMonth { get; set; }

    /// <summary>
    /// Number of items remaining (20 - purchased)
    /// </summary>
    public int ItemsRemaining { get; set; }

    /// <summary>
    /// Maximum items allowed per month (always 20)
    /// </summary>
    public int MonthlyLimit { get; set; } = 20;

    /// <summary>
    /// Start date of current billing month
    /// </summary>
    public DateTime MonthStartDate { get; set; }

    /// <summary>
    /// Date when limit will reset
    /// </summary>
    public DateTime NextResetDate { get; set; }

    /// <summary>
    /// Whether user has reached their limit
    /// </summary>
    public bool LimitReached { get; set; }
}
