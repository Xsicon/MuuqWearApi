namespace MuuqWear.Model.DTO.AdminSettingsUserDTO;

public class AdminSettingsUserDTO
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class InviteAdminSettingsUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateAdminSettingsUserDTO
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public static class AdminRoles
{
    public const string Admin = "admin";
    public const string OperationsManager = "operations_manager";
    public const string SupportTeam = "support_team";
    public const string Merchandising = "merchandising";
    public const string ContentTeam = "content_team";
    public const string TechnologySystems = "technology_systems";
    public const string SalesTeam = "sales_team";
    public const string AffiliateTeam = "affiliate_team";

    /// <summary>All six admin-portal staff roles (excludes legacy sales/affiliate team slugs).</summary>
    public static readonly string[] StaffPortal = new[]
    {
        Admin,
        OperationsManager,
        SupportTeam,
        Merchandising,
        ContentTeam,
        TechnologySystems
    };

    public static readonly string[] All = new[]
    {
        Admin,
        OperationsManager,
        SupportTeam,
        Merchandising,
        ContentTeam,
        TechnologySystems,
        SalesTeam,
        AffiliateTeam
    };

    public static bool IsStaffPortalRole(string? role) =>
        !string.IsNullOrWhiteSpace(role) && StaffPortal.Contains(role);

    public static string GetDisplayName(string role) => role switch
    {
        Admin => "Admin",
        OperationsManager => "Operations Manager",
        SupportTeam => "Customer Support",
        Merchandising => "Merchandising",
        ContentTeam => "Creative & Content",
        TechnologySystems => "Technology & Systems",
        SalesTeam => "Sales Team",
        AffiliateTeam => "Affiliate Team",
        _ => role
    };
}

public class SupabaseInviteResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid? Id { get; set; }
}
