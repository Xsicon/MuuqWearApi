using System.Security.Claims;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSettingsUserDTO;
using Xunit;

namespace MuuqWearApi.Tests.Chat;

public class ChatSessionAccessTests
{
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void IsSessionOwner_returns_true_for_matching_authenticated_user() =>
        Assert.True(ChatSessionAccess.IsSessionOwner(CustomerId, CustomerId));

    [Fact]
    public void IsSessionOwner_returns_false_for_different_authenticated_user() =>
        Assert.False(ChatSessionAccess.IsSessionOwner(CustomerId, OtherUserId));

    [Fact]
    public void IsSessionOwner_returns_true_for_guest_on_guest_session() =>
        Assert.True(ChatSessionAccess.IsSessionOwner(null, null));

    [Fact]
    public void IsSessionOwner_returns_false_for_guest_on_authenticated_session() =>
        Assert.False(ChatSessionAccess.IsSessionOwner(CustomerId, null));

    [Fact]
    public void CanAccessSession_returns_true_for_chat_admin_without_ownership() =>
        Assert.True(ChatSessionAccess.CanAccessSession(
            isChatAdmin: true,
            sessionUserId: CustomerId,
            callerUserId: OtherUserId));

    [Fact]
    public void CanAccessSession_returns_false_for_non_owner_non_admin() =>
        Assert.False(ChatSessionAccess.CanAccessSession(
            isChatAdmin: false,
            sessionUserId: CustomerId,
            callerUserId: OtherUserId));
}

public class AdminRoleClaimsTests
{
    private static ClaimsPrincipal CreateUser(string role)
    {
        var claims = new[]
        {
            new Claim(AdminRoleClaims.RoleClaimType, role)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Theory]
    [InlineData(AdminRoles.Admin, true)]
    [InlineData(AdminRoles.SupportTeam, true)]
    [InlineData(AdminRoles.Merchandising, false)]
    [InlineData(AdminRoles.OperationsManager, false)]
    public void CanActAsChatAdmin_matches_support_roles(string role, bool expected) =>
        Assert.Equal(expected, AdminRoleClaims.CanActAsChatAdmin(CreateUser(role)));
}
