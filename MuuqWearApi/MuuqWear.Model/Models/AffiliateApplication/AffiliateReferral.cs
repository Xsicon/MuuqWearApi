using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.AffiliateApplication;
[Table("affiliate_referrals")]
public class AffiliateReferral : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("affiliate_code")]
    public string AffiliateCode { get; set; } = string.Empty;

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("order_total")]
    public decimal OrderTotal { get; set; }

    [Column("commission_amount")]
    public decimal CommissionAmount { get; set; }

    [Column("commission_rate")]
    public int CommissionRate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
