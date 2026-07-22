using MuuqWear.Model.DTO.AdminSettingsUserDTO;

namespace MuuqWear.Application.Shared;

public static class AdminRoleClaims
{
    public const string RoleClaimType = "app_role";

    public static bool CanActAsChatAdmin(System.Security.Claims.ClaimsPrincipal user) =>
        user.IsInRole(AdminRoles.Admin) || user.IsInRole(AdminRoles.SupportTeam);
}
