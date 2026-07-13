namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class AffiliateTierDTO
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ItemsSoldThreshold { get; set; }
    public decimal CommissionRatePercent { get; set; }
    public decimal ReferralDiscountPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateAffiliateTierDTO
{
    public string? DisplayName { get; set; }
    public int? ItemsSoldThreshold { get; set; }
    public decimal? CommissionRatePercent { get; set; }
    public decimal? ReferralDiscountPercent { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
