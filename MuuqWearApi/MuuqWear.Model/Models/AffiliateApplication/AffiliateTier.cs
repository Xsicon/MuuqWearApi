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

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }
}
