namespace MuuqWear.Model.DTO.RefundDTO;

public class RefundDTO
{
    public Guid Id { get; set; }
    public string RefundNumber { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = RefundStatus.Pending;
    public DateTime? CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ReturnId { get; set; }
    public string? StripeRefundId { get; set; }
    public string? FailureReason { get; set; }
    public string Currency { get; set; } = "usd";
    public string? StripePaymentIntentId { get; set; }
}

public static class RefundStatus
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

public static class PaymentStatus
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
}
