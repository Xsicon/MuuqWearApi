using Microsoft.Extensions.Caching.Memory;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.CustomerDTO;
using MuuqWear.Model.DTO.ProfileDTO;
using MuuqWear.Model.Models.Profiles;

namespace MuuqWear.API.Service;

public class ProfileService : IProfileService
{
    private readonly Supabase.Client _client;
    private readonly IMemoryCache _cache;

    public ProfileService(SupabaseClientFactory factory, IMemoryCache cache)
    {
        _client = factory.CreateClient();
        _cache = cache;
    }

    public async Task<Response<ProfileDTO>> GetProfile(Guid userId)
    {
        try
        {
            var result = await _client
                .From<Profiles>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Single();

            if (result == null)
                return Response<ProfileDTO>.Fail("Profile not found");

            await AccountAccessGuard.ClearExpiredSuspensionAsync(_client, result);

            return Response<ProfileDTO>.SuccessResponse(
                MapProfile(result), "Profile fetched");
        }
        catch (Exception)
        {
            return Response<ProfileDTO>.Fail("Unable to load profile.");
        }
    }

    public async Task<Response<AccountAccessStatusDTO>> GetAccountAccessStatus(Guid userId)
    {
        try
        {
            var profile = await AccountAccessGuard.LoadProfileAsync(_client, userId);
            if (profile == null)
                return Response<AccountAccessStatusDTO>.Fail("Profile not found");

            await AccountAccessGuard.ClearExpiredSuspensionAsync(_client, profile);

            var status = AccountAccessGuard.ResolveEffectiveStatus(profile);
            var dto = new AccountAccessStatusDTO
            {
                IsActive = status == AccountStatusValues.Active,
                AccountStatus = status,
                SuspendedUntil = AccountAccessGuard.IsCurrentlySuspended(profile)
                    ? profile.SuspendedUntil
                    : null
            };

            return Response<AccountAccessStatusDTO>.SuccessResponse(
                dto,
                dto.IsActive ? "Account active" : "Account inactive");
        }
        catch (Exception)
        {
            return Response<AccountAccessStatusDTO>.Fail("Unable to load account status.");
        }
    }

    public async Task<Response<ProfileDTO>> UpdateProfile(
        Guid userId, UpdateProfileDTO request)
    {
        try
        {
            var result = await _client
                .From<Profiles>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Set(p => p.FullName!, request.FullName)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<ProfileDTO>.Fail("Failed to update profile");

            return Response<ProfileDTO>.SuccessResponse(
                MapProfile(updated), "Profile updated successfully");
        }
        catch (Exception)
        {
            return Response<ProfileDTO>.Fail("Unable to update profile.");
        }
    }

    public async Task<Response<bool>> DeleteAccount(Guid userId)
    {
        try
        {
            var result = await _client
                .From<Profiles>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Set(p => p.IsDeleted, true)
                .Set(p => p.DeletedAt!, DateTime.UtcNow)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<bool>.Fail("Failed to delete account");

            return Response<bool>.SuccessResponse(
                true, "Account deleted successfully");
        }
        catch (Exception)
        {
            return Response<bool>.Fail("Unable to delete account.");
        }
    }

    public async Task UpdateLastActive(Guid userId)
    {
        var cacheKey = ApiCacheKeys.LastActive(userId);
        if (_cache.TryGetValue(cacheKey, out _))
            return;

        await _client
            .From<Profiles>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.Equals,
                userId.ToString())
            .Set(p => p.LastActiveAt!, DateTime.UtcNow)
            .Update();

        _cache.Set(cacheKey, true, ApiCacheKeys.LastActiveThrottle);
    }

    public async Task UpdateNotificationsReadAt(Guid userId)
    {
        await _client
            .From<Profiles>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.Equals,
                userId.ToString())
            .Set(p => p.NotificationsReadAt!, DateTime.UtcNow)
            .Update();
    }

    private static ProfileDTO MapProfile(Profiles result) =>
        new()
        {
            Id = (Guid)result.Id!,
            FullName = result.FullName,
            Email = result.Email,
            Phone = result.Phone,
            IsDeleted = result.IsDeleted,
            NotificationsReadAt = result.NotificationsReadAt,
            AffiliateTier = result.AffiliateTier,
            AccountStatus = AccountAccessGuard.ResolveEffectiveStatus(result),
            SuspendedUntil = AccountAccessGuard.IsCurrentlySuspended(result)
                ? result.SuspendedUntil
                : null
        };
}
