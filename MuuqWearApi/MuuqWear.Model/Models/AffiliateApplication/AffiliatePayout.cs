using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AffiliateApplication;

[Table("affiliate_payouts")]
public class AffiliatePayout : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("affiliate_code")]
    public string AffiliateCode { get; set; } = string.Empty;

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("referral_count")]
    public int ReferralCount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "completed";

    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("admin_notes")]
    public string? AdminNotes { get; set; }

    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; }

    [Column("processed_by")]
    public Guid ProcessedBy { get; set; }
}
