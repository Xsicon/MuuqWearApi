namespace MuuqWear.Application.Shared;

/// <summary>
/// Authorization policy names aligned with MuuqWear admin portal sections.
/// JWT role claim: <see cref="AdminRoleClaims.RoleClaimType"/> (app_role).
/// </summary>
public static class AdminAuthorizationPolicies
{
    public const string AdminOrders = "AdminOrders";
    public const string AdminProducts = "AdminProducts";
    public const string AdminContent = "AdminContent";
    public const string AdminAffiliates = "AdminAffiliates";
    public const string AdminSupport = "AdminSupport";
    public const string AdminCareers = "AdminCareers";
    public const string AdminSystem = "AdminSystem";
    public const string AdminCustomers = "AdminCustomers";
    public const string AdminCustomerNotesRead = "AdminCustomerNotesRead";
    public const string AdminOnly = "AdminOnly";
    public const string StaffPortal = "StaffPortal";
    public const string AdminAnalytics = "AdminAnalytics";
}
