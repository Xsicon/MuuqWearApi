using Microsoft.Extensions.Configuration;
using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO;
using MuuqWear.Model.DTO.AdminSettingsUserDTO;
using MuuqWear.Model.DTO.SupaBaseHealthDTO;
using MuuqWear.Model.Models;
using System.Net.Http.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MuuqWear.Application.Service;

public class AdminSettingService : IAdminSettingService
{
    private readonly Supabase.Client _client;
    private readonly Supabase.Client _adminClient;
    private readonly IConfiguration _configuration;

    public AdminSettingService(
        SupabaseClientFactory factory,
        SupabaseAdminClientFactory adminFactory, IConfiguration configuration)
    {
        _client = factory.CreateClient();
        _adminClient = adminFactory.CreateClient();
        _configuration = configuration;
    }

    // =============================================
    // GET ALL ADMIN USERS
    // =============================================
    public async Task<Response<List<AdminSettingsUserDTO>>> GetAll()
    {
        try
        {
            var result = await _client
                .From<Profiles>()
                .Filter("role",
                    Supabase.Postgrest.Constants.Operator.NotEqual,
                    "user")
                .Filter("is_deleted",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    "false")
                .Order("created_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var users = result.Models.Select(p => new AdminSettingsUserDTO
            {
                Id = (Guid)p.Id!,
                FullName = p.FullName,
                Email = p.Email,
                Role = p.Role,
                CreatedAt = p.CreatedAt,
                LastActiveAt = p.LastActiveAt,
                IsDeleted = p.IsDeleted
            }).ToList();

            return Response<List<AdminSettingsUserDTO>>
                .SuccessResponse(users, "Admin users fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AdminSettingsUserDTO>>
                .Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // INVITE ADMIN USER
    // =============================================
    public async Task<Response<AdminSettingsUserDTO>> Invite(
     InviteAdminSettingsUserDTO request)
    {
        try
        {
            // Step 1 — validate role
            if (!AdminRoles.All.Contains(request.Role))
                return Response<AdminSettingsUserDTO>.Fail("Invalid role");

            // Step 2 — check email not already registered
            var existing = await _client
                .From<Profiles>()
                .Filter("email",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.Email.Trim().ToLower())
                .Get();

            if (existing.Models.Any())
                return Response<AdminSettingsUserDTO>.Fail(
                    "A user with this email already exists");

            // Step 3 — invite via Supabase Admin REST API directly
            //  SDK v1.1.1 does not expose Admin API
            //  direct HTTP call to Supabase invite endpoint
            var supabaseUrl = _configuration["SupaBase:Url"]!;
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]!;

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", serviceRoleKey);

            var inviteResult = await http.PostAsJsonAsync(
                $"{supabaseUrl}/auth/v1/invite",
                new { email = request.Email.Trim().ToLower() });

            if (!inviteResult.IsSuccessStatusCode)
            {
                var error = await inviteResult.Content.ReadAsStringAsync();
                return Response<AdminSettingsUserDTO>.Fail(
                    "Failed to invite user. Please try again.");
            }

            // Step 4 — read invited user id from response
            var inviteResponse = await inviteResult.Content
                .ReadFromJsonAsync<SupabaseInviteResponse>();

            if (inviteResponse?.Id == null)
                return Response<AdminSettingsUserDTO>.Fail("Failed to get user id from invite");

            // Step 5 — create profile row using admin client
            var profile = new Profiles
            {
                Id = inviteResponse.Id.Value, // ← now safe 
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLower(),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            var profileResult = await _adminClient
                .From<Profiles>()
                .Insert(profile);

            var inserted = profileResult.Models.FirstOrDefault();
            if (inserted == null)
                return Response<AdminSettingsUserDTO>.Fail(
                    "User invited but profile creation failed");

            return Response<AdminSettingsUserDTO>.SuccessResponse(
                new AdminSettingsUserDTO
                {
                    Id = (Guid)inserted.Id!,
                    FullName = inserted.FullName,
                    Email = inserted.Email,
                    Role = inserted.Role,
                    CreatedAt = inserted.CreatedAt
                }, "User invited successfully. " +
                   "An invitation email has been sent.");
        }
        catch (Exception ex)
        {
            return Response<AdminSettingsUserDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE ADMIN USER
    // =============================================
    public async Task<Response<AdminSettingsUserDTO>> Update(
        Guid userId, UpdateAdminSettingsUserDTO request)
    {
        try
        {
            if (!AdminRoles.All.Contains(request.Role))
                return Response<AdminSettingsUserDTO>.Fail("Invalid role");

            var result = await _client
                .From<Profiles>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Set(p => p.FullName!, request.FullName.Trim())
                .Set(p => p.Role!, request.Role)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<AdminSettingsUserDTO>.Fail("User not found");

            return Response<AdminSettingsUserDTO>.SuccessResponse(
                new AdminSettingsUserDTO
                {
                    Id = (Guid)updated.Id!,
                    FullName = updated.FullName,
                    Email = updated.Email,
                    Role = updated.Role,
                    CreatedAt = updated.CreatedAt,
                    LastActiveAt = updated.LastActiveAt
                }, "User updated successfully");
        }
        catch (Exception ex)
        {
            return Response<AdminSettingsUserDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // DEACTIVATE ADMIN USER
    // =============================================
    public async Task<Response<bool>> Deactivate(Guid userId)
    {
        try
        {
            await _client
                .From<Profiles>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Set(p => p.IsDeleted, true)
                .Set(p => p.DeletedAt!, DateTime.UtcNow)
                .Update();

            return Response<bool>.SuccessResponse(
                true, "User deactivated successfully");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CHECK SUPABASE HEALTH
    // =============================================
    public async Task<Response<SupabaseHealthDTO>> CheckSupabaseHealth()
    {
        try
        {
            //lightweight query — fetch one row to verify DB reachable
            await _client.From<Order>().Limit(1).Get();

            return Response<SupabaseHealthDTO>.SuccessResponse(
                new SupabaseHealthDTO
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    CheckedAt = DateTime.UtcNow
                }, "Supabase is healthy");
        }
        catch (Exception)
        {
            return Response<SupabaseHealthDTO>.SuccessResponse(
                new SupabaseHealthDTO
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    CheckedAt = DateTime.UtcNow
                }, "Supabase is unhealthy");
        }
    }
}
