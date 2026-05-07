namespace MuuqWear.Model.DTO.OrdeReturnDTO;

// ─── RETURN LIST + DETAIL DTO ────────────────────────────────
// used for both list view and detail
public class OrderReturnDTO
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ItemsToReturn { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
}

// ─── SUBMIT RETURN DTO ───────────────────────────────────────
// used when user submits return form
public class SubmitReturnDTO
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ItemsToReturn { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

// ─── UPDATE RETURN STATUS DTO ────────────────────────────────
// used by admin approve/deny
public class UpdateReturnStatusDTO
{
    public string Status { get; set; } = string.Empty;
}

// ─── RETURN STATUS CONSTANTS ─────────────────────────────────
public static class ReturnStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Denied = "denied";
}
