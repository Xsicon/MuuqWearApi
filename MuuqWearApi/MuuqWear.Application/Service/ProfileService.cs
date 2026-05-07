using MuuqWear.API.Interfaces;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.ProfileDTO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MuuqWear.API.Service;

public class ProfileService : IProfileService
{
    private readonly Supabase.Client _client;

    public ProfileService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================================
    // GET PROFILE
    // =============================================
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

            var profileDTO = new ProfileDTO
            {
                Id = (Guid)result.Id!,
                FullName = result.FullName,
                Email = result.Email,
                Phone = result.Phone,
                IsDeleted = result.IsDeleted
            };

            return Response<ProfileDTO>.SuccessResponse(
                profileDTO, "Profile fetched");
        }
        catch (Exception ex)
        {
            return Response<ProfileDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE PROFILE
    // =============================================
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

            var profileDTO = new ProfileDTO
            {
                Id = (Guid)updated.Id!,
                FullName = updated.FullName,
                Email = updated.Email,
                Phone = updated.Phone
            };

            return Response<ProfileDTO>.SuccessResponse(
                profileDTO, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            return Response<ProfileDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // DELETE ACCOUNT (soft delete)
    // =============================================
    public async Task<Response<bool>> DeleteAccount(Guid userId)
    {
        try
        {
            // soft delete — just mark as deleted
            // real data preserved for records 
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
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE LAST ACTIVE
    // =============================================
    public async Task UpdateLastActive(Guid userId)
    {
        await _client
            .From<Profiles>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.Equals,
                userId.ToString())
            .Set(p => p.LastActiveAt!, DateTime.UtcNow)
            .Update();
    }

}