namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class AffiliatePendingPayoutDTO
{
    public string AffiliateCode { get; set; } = string.Empty;
    public string AffiliateName { get; set; } = string.Empty;
    public string AffiliateTier { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ReferralCount { get; set; }
    public DateTime OldestPendingDate { get; set; }
    public string Status { get; set; } = "pending";
    public string FormattedDate => OldestPendingDate.ToString("MMM dd, yyyy");
    public string FormattedAmount => $"${TotalAmount:F2}";
}

public class AffiliatePendingReferralDTO
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public decimal CommissionAmount { get; set; }
    public int CommissionRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MaskedCustomer { get; set; } = string.Empty;
}

public class ProcessAffiliatePayoutDTO
{
    public string? PaymentMethod { get; set; }
    public string? AdminNotes { get; set; }
}

public class AffiliatePayoutResultDTO
{
    public Guid PayoutId { get; set; }
    public string AffiliateCode { get; set; } = string.Empty;
    public string AffiliateName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ReferralCount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = "completed";
    public string? PaymentMethod { get; set; }
}
