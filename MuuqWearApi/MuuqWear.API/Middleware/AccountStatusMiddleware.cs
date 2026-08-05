using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MuuqWear.API.Shared;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.CustomerDTO;

namespace MuuqWear.API.Middleware;

/// <summary>
/// Blocks authenticated API calls when the JWT subject is deleted or currently suspended.
/// </summary>
public class AccountStatusMiddleware
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(20);
    private readonly RequestDelegate _next;

    public AccountStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        SupabaseAdminClientFactory adminFactory,
        IMemoryCache cache)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = ResolveUserId(context.User);
        if (userId == Guid.Empty)
        {
            await _next(context);
            return;
        }

        var cacheKey = $"account-access:{userId}";
        if (!cache.TryGetValue(cacheKey, out AccessSnapshot? snapshot) || snapshot == null)
        {
            var client = adminFactory.CreateClient();
            var (blocked, message, accountStatus, suspendedUntil) =
                await AccountAccessGuard.EvaluateAccessAsync(client, userId);

            snapshot = new AccessSnapshot(blocked, message, accountStatus, suspendedUntil);
            cache.Set(cacheKey, snapshot, CacheDuration);
        }

        if (snapshot.Blocked)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                Response<object>.Fail(
                    snapshot.Message ?? "Account access denied.",
                    new
                    {
                        accountStatus = snapshot.AccountStatus,
                        suspendedUntil = snapshot.SuspendedUntil
                    }),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkip(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
            return true;

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/Auth", StringComparison.OrdinalIgnoreCase))
            return true;

        // Allow clients to poll status while suspended/deleted.
        if (path.StartsWith("/api/Profile/is-active", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/Profile/last-active", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static Guid ResolveUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private sealed record AccessSnapshot(
        bool Blocked,
        string? Message,
        string AccountStatus,
        DateTime? SuspendedUntil);
}
