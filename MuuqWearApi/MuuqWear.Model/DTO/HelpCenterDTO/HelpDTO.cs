namespace MuuqWear.Model.DTO.HelpCenterDTO;


// ─── SUPPORT TICKET DTO ──────────────────────────────────────
public class SupportTicketDTO
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// ─── SUBMIT TICKET DTO ───────────────────────────────────────
public class SubmitTicketDTO
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ─── UPDATE TICKET STATUS DTO ────────────────────────────────
public class UpdateTicketStatusDTO
{
    public string Status { get; set; } = string.Empty;
}

// ─── TICKET STATS DTO ────────────────────────────────────────
public class TicketStatsDTO
{
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int TotalCount { get; set; }
}

// ─── TICKET STATUS CONSTANTS ─────────────────────────────────
public static class TicketStatus
{
    public const string Open = "open";
    public const string InProgress = "in_progress";
    public const string Resolved = "resolved";

    public static readonly string[] All = new[]
    {
        Open, InProgress, Resolved
    };
}

// ─── TICKET PRIORITY CONSTANTS ───────────────────────────────
public static class TicketPriority
{
    public const string High = "high";
    public const string Normal = "normal";

    // ✅ auto-assign priority based on category
    public static string FromCategory(string category) =>
        category.ToLower() switch
        {
            "orders" => High,
            "shipping" => High,
            "payments" => High,
            "returns" => High,
            _ => Normal
        };
}

// ─── TICKET CATEGORIES ───────────────────────────────────────
public static class TicketCategory
{
    public const string Orders = "Orders";
    public const string Shipping = "Shipping";
    public const string Returns = "Returns";
    public const string Payments = "Payments";
    public const string Account = "Account";
    public const string ProductInfo = "Product Info";
}
