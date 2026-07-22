using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSettingsUserDTO;

namespace MuuqWear.API.Authorization;

public static class AdminAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAdminAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminAuthorizationPolicies.AdminOrders, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.OperationsManager));

            options.AddPolicy(AdminAuthorizationPolicies.AdminProducts, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.Merchandising));

            options.AddPolicy(AdminAuthorizationPolicies.AdminContent, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.ContentTeam));

            options.AddPolicy(AdminAuthorizationPolicies.AdminAffiliates, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.OperationsManager));

            options.AddPolicy(AdminAuthorizationPolicies.AdminSupport, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.SupportTeam));

            options.AddPolicy(AdminAuthorizationPolicies.AdminCareers, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.OperationsManager));

            options.AddPolicy(AdminAuthorizationPolicies.AdminSystem, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.TechnologySystems));

            options.AddPolicy(AdminAuthorizationPolicies.AdminCustomers, policy =>
                policy.RequireRole(AdminRoles.Admin));

            options.AddPolicy(AdminAuthorizationPolicies.AdminCustomerNotesRead, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.SupportTeam));

            options.AddPolicy(AdminAuthorizationPolicies.AdminOnly, policy =>
                policy.RequireRole(AdminRoles.Admin));

            options.AddPolicy(AdminAuthorizationPolicies.StaffPortal, policy =>
                policy.RequireRole(AdminRoles.StaffPortal));

            options.AddPolicy(AdminAuthorizationPolicies.AdminAnalytics, policy =>
                policy.RequireRole(AdminRoles.Admin, AdminRoles.OperationsManager));
        });

        return services;
    }
}
