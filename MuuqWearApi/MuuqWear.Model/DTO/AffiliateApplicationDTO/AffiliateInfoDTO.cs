namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class AffiliateInfoDTO
{
    public string AffiliateCode { get; set; } = string.Empty;
    public string AffiliateLink { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int ItemsSold { get; set; }
    public decimal CommissionEarned { get; set; }
    public decimal BonusEarned { get; set; }
    public int TotalClicks { get; set; } // Will implement in Task 1F
    public int Conversions { get; set; }
    public decimal CommissionThisMonth { get; set; }  // Commission earned this month
    public decimal CommissionPending { get; set; }
}
