using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MuuqWear.Model.Models.Order;

[Table("refunds")]
public class Refund : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("refund_number")]
    public string RefundNumber { get; set; } = string.Empty;

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("return_id")]
    public Guid? ReturnId { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "usd";

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("stripe_payment_intent_id")]
    public string? StripePaymentIntentId { get; set; }

    [Column("stripe_refund_id")]
    public string? StripeRefundId { get; set; }

    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }
}
