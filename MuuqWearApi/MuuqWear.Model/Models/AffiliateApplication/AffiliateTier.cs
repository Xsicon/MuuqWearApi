using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AffiliateApplication;

[Table("affiliate_tiers")]
public class AffiliateTier : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("items_sold_threshold")]
    public int ItemsSoldThreshold { get; set; }

    [Column("commission_rate_percent")]
    public decimal CommissionRatePercent { get; set; }

    [Column("referral_discount_percent")]
    public decimal ReferralDiscountPercent { get; set; }

    [Column("quarterly_bonus_percent")]
    public decimal QuarterlyBonusPercent { get; set; }

    [Column("max_affiliates")]
    public int? MaxAffiliates { get; set; }

    [Column("perks")]
    public List<string> Perks { get; set; } = new();

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }
}
