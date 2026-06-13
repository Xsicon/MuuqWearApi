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
    public const string SupportTeam = "support_team";
    public const string SalesTeam = "sales_team";
    public const string ContentTeam = "content_team";
    public const string AffiliateTeam = "affiliate_team";

    public static readonly string[] All = new[]
    {
        Admin,
        SupportTeam,
        SalesTeam,
        ContentTeam,
        AffiliateTeam
    };

    public static string GetDisplayName(string role) => role switch
    {
        Admin => "Admin",
        SupportTeam => "Support Team",
        SalesTeam => "Sales Team",
        ContentTeam => "Content Team",
        AffiliateTeam => "Affiliate Team",
        _ => role
    };
}

public class SupabaseInviteResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid? Id { get; set; }
}
