namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class RecentReferralDTO
{
    /// <summary>
    /// When the referral was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Masked customer identifier (e.g., "***4821")
    /// </summary>
    public string MaskedCustomer { get; set; } = string.Empty;

    /// <summary>
    /// Total order value
    /// </summary>
    public decimal OrderTotal { get; set; }

    /// <summary>
    /// Commission earned
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Referral status (pending, completed, cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Formatted date for display (e.g., "Mar 22, 2026")
    /// </summary>
    public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy");
}
