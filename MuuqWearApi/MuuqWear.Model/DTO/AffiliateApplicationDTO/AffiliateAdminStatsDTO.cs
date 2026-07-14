namespace MuuqWear.Model.DTO.AffiliateApplicationDTO;

public class AffiliateAdminStatsDTO
{
    public int PendingApplications { get; set; }
    public int WaitlistedApplications { get; set; }
    public int ApplicationsThisMonth { get; set; }
    public int ActiveAffiliates { get; set; }
    public int InactiveAffiliates { get; set; }
    public Dictionary<string, int> AffiliatesByTier { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal TotalCommissionsPaid { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal PendingPayoutAmount { get; set; }
    public int PendingPayoutCount { get; set; }
    public decimal ProcessedThisMonthAmount { get; set; }
    public int ProcessedThisMonthCount { get; set; }
    public decimal TotalDisbursedYtd { get; set; }
}

public class UpdateAffiliateActiveStatusDTO
{
    public bool IsActive { get; set; }
}

public class BulkProcessAffiliatePayoutsDTO
{
    public string? PaymentMethod { get; set; }
    public string? AdminNotes { get; set; }
}

public class BulkAffiliatePayoutResultDTO
{
    public int ProcessedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<Guid> PayoutIds { get; set; } = new();
    public List<string> FailedCodes { get; set; } = new();
}
