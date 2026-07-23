using System.Security.Claims;
using MuuqWear.Model.DTO.AdminSettingsUserDTO;

namespace MuuqWear.Application.Shared;

public static class AdminRoleClaims
{
    public const string RoleClaimType = "app_role";

    public static bool CanActAsChatAdmin(ClaimsPrincipal user)
    {
        if (user.IsInRole(AdminRoles.Admin) || user.IsInRole(AdminRoles.SupportTeam))
            return true;

        // Fallback if RoleClaimType was not applied to the JWT bearer options.
        foreach (var claim in user.FindAll(RoleClaimType).Concat(user.FindAll(ClaimTypes.Role)))
        {
            if (string.Equals(claim.Value, AdminRoles.Admin, StringComparison.OrdinalIgnoreCase)
                || string.Equals(claim.Value, AdminRoles.SupportTeam, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
