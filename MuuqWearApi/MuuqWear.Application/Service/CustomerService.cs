using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.CustomerDTO;
using MuuqWear.Model.Models.Profiles;
using System.Text.Json;
using CustomerNoteRecord = MuuqWear.Model.Models.CustomerNote.CustomerNote;

namespace MuuqWear.Application.Service;

public class CustomerService : ICustomerService
{
    private const int NotePreviewMaxLength = 120;
    private const string CustomerNotFoundMessage = "Customer not found";

    private readonly Supabase.Client _client;
    private readonly Supabase.Client _adminClient;

    public CustomerService(
        SupabaseClientFactory factory,
        SupabaseAdminClientFactory adminFactory)
    {
        _client = factory.CreateClient();
        _adminClient = adminFactory.CreateClient();
    }

    public async Task<Response<PaginatedResponse<CustomerDTO>>> GetAll(
        string? search, int page, int pageSize, string? status = null)
    {
        try
        {
            var searchTerm = search?.Trim() ?? "";
            var offset = (page - 1) * pageSize;
            var statusFilter = NormalizeStatusFilter(status);

            if (statusFilter != null)
                return await GetAllFilteredByStatus(searchTerm, statusFilter, page, pageSize, offset);

            var countResult = await _client.Rpc(
                "get_customers_count",
                new Dictionary<string, object>
                {
                    { "p_search_term", searchTerm }
                });

            var totalCount = 0;
            int.TryParse(countResult.Content?.Trim('"'), out totalCount);

            var dataResult = await _client.Rpc(
                "get_customers",
                new Dictionary<string, object>
                {
                    { "p_search_term", searchTerm },
                    { "p_page_size", pageSize },
                    { "p_offset", offset }
                });

            var customers = DeserializeCustomers(dataResult.Content);
            await EnrichCustomersAsync(customers);

            return SuccessPage(customers, totalCount, page, pageSize);
        }
        catch (Exception)
        {
            return Response<PaginatedResponse<CustomerDTO>>
                .Fail("Unable to load customers.");
        }
    }

    public async Task<Response<CustomerDTO>> GetById(Guid customerId)
    {
        try
        {
            var profile = await GetProfileByIdAsync(customerId);
            if (profile == null)
                return Response<CustomerDTO>.Fail(CustomerNotFoundMessage);

            await AccountAccessGuard.ClearExpiredSuspensionAsync(_adminClient, profile);

            var dto = MapProfileToCustomerDto(profile);
            await EnrichCustomersAsync([dto]);
            return Response<CustomerDTO>.SuccessResponse(dto, "Customer fetched");
        }
        catch (Exception)
        {
            return Response<CustomerDTO>.Fail("Unable to load customer.");
        }
    }

    public async Task<Response<CustomerDTO>> Suspend(
        Guid customerId, SuspendCustomerDTO request, Guid adminUserId)
    {
        try
        {
            if (request == null)
                return Response<CustomerDTO>.Fail("Request body is required");

            if (!AccountSuspension.IsAllowedDuration(request.DurationDays))
            {
                return Response<CustomerDTO>.Fail(
                    "DurationDays must be one of: 1, 3, 7, 14, 30, 60, 90, 180, 365");
            }

            var reason = NormalizeReason(request.Reason);
            if (reason != null && reason.Length > AccountSuspension.MaxReasonLength)
            {
                return Response<CustomerDTO>.Fail(
                    $"Reason must be {AccountSuspension.MaxReasonLength} characters or fewer");
            }

            var profile = await GetProfileByIdAsync(customerId);
            if (profile == null)
                return Response<CustomerDTO>.Fail(CustomerNotFoundMessage);

            if (profile.IsDeleted)
                return Response<CustomerDTO>.Fail("Cannot suspend a deleted account");

            await AccountAccessGuard.ClearExpiredSuspensionAsync(_adminClient, profile);

            var now = DateTime.UtcNow;
            // Re-suspend always recalculates from now (does not stack onto prior SuspendedUntil).
            var until = AccountSuspension.ComputeSuspendedUntil(request.DurationDays, now);

            var result = await _adminClient
                .From<Profiles>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    customerId.ToString())
                .Set(p => p.AccountStatus, AccountStatusValues.Suspended)
                .Set(p => p.SuspendedUntil!, until)
                .Set(p => p.SuspensionReason!, reason)
                .Set(p => p.SuspendedByUserId!, adminUserId)
                .Set(p => p.SuspendedAt!, now)
                .Set(p => p.ReactivatedAt!, null)
                .Set(p => p.ReactivatedByUserId!, null)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<CustomerDTO>.Fail("Failed to suspend customer");

            var noteBody = reason == null
                ? $"Account suspended for {request.DurationDays} days."
                : $"Account suspended for {request.DurationDays} days. Reason: {reason}";
            await TryCreateSystemNote(customerId, noteBody, adminUserId);

            var dto = MapProfileToCustomerDto(updated);
            await EnrichCustomersAsync([dto]);
            return Response<CustomerDTO>.SuccessResponse(dto, "Customer suspended");
        }
        catch (Exception)
        {
            return Response<CustomerDTO>.Fail("Unable to suspend customer.");
        }
    }

    public async Task<Response<CustomerDTO>> Reactivate(
        Guid customerId, ReactivateCustomerDTO? request, Guid adminUserId)
    {
        try
        {
            var profile = await GetProfileByIdAsync(customerId);
            if (profile == null)
                return Response<CustomerDTO>.Fail(CustomerNotFoundMessage);

            if (profile.IsDeleted)
                return Response<CustomerDTO>.Fail("Cannot reactivate a deleted account");

            var reason = NormalizeReason(request?.Reason);
            if (reason != null && reason.Length > AccountSuspension.MaxReasonLength)
            {
                return Response<CustomerDTO>.Fail(
                    $"Reason must be {AccountSuspension.MaxReasonLength} characters or fewer");
            }

            var now = DateTime.UtcNow;
            var result = await _adminClient
                .From<Profiles>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    customerId.ToString())
                .Set(p => p.AccountStatus, AccountStatusValues.Active)
                .Set(p => p.SuspendedUntil!, null)
                .Set(p => p.SuspensionReason!, null)
                .Set(p => p.SuspendedByUserId!, null)
                .Set(p => p.SuspendedAt!, null)
                .Set(p => p.ReactivatedAt!, now)
                .Set(p => p.ReactivatedByUserId!, adminUserId)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<CustomerDTO>.Fail("Failed to reactivate customer");

            var noteBody = reason == null
                ? "Account reactivated by admin."
                : $"Account reactivated by admin. Reason: {reason}";
            await TryCreateSystemNote(customerId, noteBody, adminUserId);

            var dto = MapProfileToCustomerDto(updated);
            await EnrichCustomersAsync([dto]);
            return Response<CustomerDTO>.SuccessResponse(dto, "Customer reactivated");
        }
        catch (Exception)
        {
            return Response<CustomerDTO>.Fail("Unable to reactivate customer.");
        }
    }

    public async Task<Response<List<CustomerNoteDTO>>> GetNotes(Guid customerId)
    {
        try
        {
            if (!await CustomerExistsAsync(customerId))
                return Response<List<CustomerNoteDTO>>.Fail(CustomerNotFoundMessage);

            var result = await _adminClient
                .From<CustomerNoteRecord>()
                .Filter("customer_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    customerId.ToString())
                .Order("created_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var notes = result.Models.ToList();
            var authorProfiles = await GetProfilesByIdsAsync(
                notes.Select(n => n.AuthorUserId));

            var profileMap = authorProfiles
                .Where(p => p.Id.HasValue)
                .ToDictionary(p => p.Id!.Value);

            var dtos = notes
                .Select(n => MapNoteToDto(
                    n,
                    profileMap.GetValueOrDefault(n.AuthorUserId)))
                .ToList();

            return Response<List<CustomerNoteDTO>>
                .SuccessResponse(dtos, "Notes fetched");
        }
        catch (Exception)
        {
            return Response<List<CustomerNoteDTO>>
                .Fail("Unable to load notes.");
        }
    }

    public async Task<Response<CustomerNoteDTO>> CreateNote(
        Guid customerId, string body, Guid authorUserId)
    {
        try
        {
            var trimmedBody = body?.Trim() ?? "";

            if (trimmedBody.Length < 1 || trimmedBody.Length > 4000)
            {
                return Response<CustomerNoteDTO>.Fail(
                    "Note body must be between 1 and 4000 characters");
            }

            if (!await CustomerExistsAsync(customerId))
                return Response<CustomerNoteDTO>.Fail(CustomerNotFoundMessage);

            var authorProfile = await GetProfileByIdAsync(authorUserId);
            if (authorProfile == null)
                return Response<CustomerNoteDTO>.Fail("Author profile not found");

            var now = DateTime.UtcNow;
            var note = new CustomerNoteRecord
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                AuthorUserId = authorUserId,
                AuthorName = ResolveAuthorName(authorProfile, null),
                AuthorRole = FormatAuthorRole(authorProfile.Role),
                Body = trimmedBody,
                CreatedAt = now,
                UpdatedAt = null
            };

            var insertResult = await _adminClient
                .From<CustomerNoteRecord>()
                .Insert(note);

            var inserted = insertResult.Models.FirstOrDefault() ?? note;

            return Response<CustomerNoteDTO>.SuccessResponse(
                MapNoteToDto(inserted, authorProfile), "Note created");
        }
        catch (Exception)
        {
            return Response<CustomerNoteDTO>.Fail("Unable to create note.");
        }
    }

    private async Task<Response<PaginatedResponse<CustomerDTO>>> GetAllFilteredByStatus(
        string searchTerm,
        string statusFilter,
        int page,
        int pageSize,
        int offset)
    {
        var profilesResult = await _adminClient
            .From<Profiles>()
            .Filter("is_deleted",
                Supabase.Postgrest.Constants.Operator.Equals,
                "false")
            .Get();

        var now = DateTime.UtcNow;
        IEnumerable<Profiles> filtered = profilesResult.Models
            .Where(p => IsCustomerRole(p.Role));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filtered = filtered.Where(p =>
                (p.FullName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        filtered = statusFilter == AccountStatusValues.Suspended
            ? filtered.Where(p => AccountAccessGuard.IsCurrentlySuspended(p, now))
            : filtered.Where(p => !AccountAccessGuard.IsCurrentlySuspended(p, now));

        var materialized = filtered
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        var totalCount = materialized.Count;
        var pageProfiles = materialized.Skip(offset).Take(pageSize).ToList();
        var customers = pageProfiles.Select(MapProfileToCustomerDto).ToList();

        // Merge order stats from the unfiltered RPC when available.
        var rpcCustomers = await TryLoadRpcCustomers(searchTerm, Math.Max(totalCount, pageSize));
        var rpcMap = rpcCustomers.ToDictionary(c => c.Id);
        foreach (var customer in customers)
        {
            if (rpcMap.TryGetValue(customer.Id, out var rpc))
            {
                customer.OrderCount = rpc.OrderCount;
                customer.TotalSpent = rpc.TotalSpent;
                customer.LastOrderAt = rpc.LastOrderAt;
            }
        }

        await EnrichCustomersAsync(customers);
        return SuccessPage(customers, totalCount, page, pageSize);
    }

    private async Task EnrichCustomersAsync(List<CustomerDTO> customers)
    {
        var customerIds = customers
            .Select(c => c.Id)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (customerIds.Count == 0)
            return;

        var profiles = await GetProfilesByIdsAsync(customerIds);
        var profileMap = profiles
            .Where(p => p.Id.HasValue)
            .ToDictionary(p => p.Id!.Value);

        foreach (var customer in customers)
        {
            if (!profileMap.TryGetValue(customer.Id, out var profile))
                continue;

            await AccountAccessGuard.ClearExpiredSuspensionAsync(_adminClient, profile);
            ApplyAccountFields(customer, profile);
        }

        var summaries = await GetNoteSummariesForCustomers(customerIds);
        foreach (var customer in customers)
        {
            if (!summaries.TryGetValue(customer.Id, out var summary))
                continue;

            customer.NoteCount = summary.NoteCount;
            customer.LatestNotePreview = summary.LatestNotePreview;
            customer.LatestNoteAt = summary.LatestNoteAt;
            customer.LatestNoteAuthorName = summary.LatestNoteAuthorName;
            customer.LatestNoteAuthorRole = summary.LatestNoteAuthorRole;
        }
    }

    private async Task<List<CustomerDTO>> TryLoadRpcCustomers(string searchTerm, int pageSize)
    {
        try
        {
            var dataResult = await _client.Rpc(
                "get_customers",
                new Dictionary<string, object>
                {
                    { "p_search_term", searchTerm },
                    { "p_page_size", Math.Clamp(pageSize, 1, 500) },
                    { "p_offset", 0 }
                });

            return DeserializeCustomers(dataResult.Content);
        }
        catch
        {
            return [];
        }
    }

    private async Task TryCreateSystemNote(Guid customerId, string body, Guid authorUserId)
    {
        try
        {
            await CreateNote(customerId, body, authorUserId);
        }
        catch
        {
            // Audit note is best-effort.
        }
    }

    private static List<CustomerDTO> DeserializeCustomers(string? content)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        return JsonSerializer.Deserialize<List<CustomerDTO>>(content ?? "[]", options)
               ?? [];
    }

    private static Response<PaginatedResponse<CustomerDTO>> SuccessPage(
        List<CustomerDTO> customers, int totalCount, int page, int pageSize)
    {
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / pageSize);

        return Response<PaginatedResponse<CustomerDTO>>.SuccessResponse(
            new PaginatedResponse<CustomerDTO>
            {
                Data = customers,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasMore = page < totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            },
            "Customers fetched");
    }

    private static CustomerDTO MapProfileToCustomerDto(Profiles profile) =>
        new()
        {
            Id = profile.Id ?? Guid.Empty,
            FullName = profile.FullName,
            Email = profile.Email,
            CreatedAt = profile.CreatedAt,
            AccountStatus = AccountAccessGuard.ResolveEffectiveStatus(profile),
            SuspendedUntil = AccountAccessGuard.IsCurrentlySuspended(profile)
                ? profile.SuspendedUntil
                : null,
            SuspensionReason = profile.SuspensionReason,
            SuspendedAt = profile.SuspendedAt
        };

    private static void ApplyAccountFields(CustomerDTO customer, Profiles profile)
    {
        var effective = AccountAccessGuard.ResolveEffectiveStatus(profile);
        customer.AccountStatus = effective == AccountStatusValues.Deleted
            ? AccountStatusValues.Deleted
            : effective;
        customer.SuspendedUntil = AccountAccessGuard.IsCurrentlySuspended(profile)
            ? profile.SuspendedUntil
            : null;
        customer.SuspensionReason = profile.SuspensionReason;
        customer.SuspendedAt = profile.SuspendedAt;
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var normalized = status.Trim().ToLowerInvariant();
        return normalized is AccountStatusValues.Active or AccountStatusValues.Suspended
            ? normalized
            : null;
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;
        return reason.Trim();
    }

    private static bool IsCustomerRole(string? role) =>
        string.IsNullOrWhiteSpace(role)
        || role.Equals("user", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> CustomerExistsAsync(Guid customerId)
    {
        var profile = await GetProfileByIdAsync(customerId);
        return profile != null && !profile.IsDeleted;
    }

    private async Task<Profiles?> GetProfileByIdAsync(Guid userId)
    {
        return await AccountAccessGuard.LoadProfileAsync(_adminClient, userId);
    }

    private async Task<List<Profiles>> GetProfilesByIdsAsync(
        IEnumerable<Guid> userIds)
    {
        var ids = userIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        var idFilters = ids
            .Select(id => (object)id.ToString())
            .ToList();

        var result = await _adminClient
            .From<Profiles>()
            .Filter("id",
                Supabase.Postgrest.Constants.Operator.In,
                idFilters)
            .Get();

        return result.Models;
    }

    private async Task<Dictionary<Guid, CustomerNoteSummary>> GetNoteSummariesForCustomers(
        IReadOnlyList<Guid> customerIds)
    {
        var summaries = customerIds.ToDictionary(
            id => id,
            _ => new CustomerNoteSummary());

        if (customerIds.Count == 0)
            return summaries;

        var idFilters = customerIds
            .Select(id => (object)id.ToString())
            .ToList();

        var result = await _adminClient
            .From<CustomerNoteRecord>()
            .Filter("customer_id",
                Supabase.Postgrest.Constants.Operator.In,
                idFilters)
            .Order("created_at",
                Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        var grouped = result.Models
            .GroupBy(n => n.CustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var latestNotes = grouped
            .Where(kv => summaries.ContainsKey(kv.Key))
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.OrderByDescending(n => n.CreatedAt).First());

        var authorProfiles = await GetProfilesByIdsAsync(
            latestNotes.Values.Select(n => n.AuthorUserId));

        var profileMap = authorProfiles
            .Where(p => p.Id.HasValue)
            .ToDictionary(p => p.Id!.Value);

        foreach (var (customerId, latest) in latestNotes)
        {
            profileMap.TryGetValue(latest.AuthorUserId, out var authorProfile);

            summaries[customerId] = new CustomerNoteSummary
            {
                NoteCount = grouped[customerId].Count,
                LatestNotePreview = BuildPreview(latest.Body),
                LatestNoteAt = latest.CreatedAt,
                LatestNoteAuthorName = ResolveAuthorName(
                    authorProfile, latest.AuthorName),
                LatestNoteAuthorRole = ResolveAuthorRole(
                    authorProfile, latest.AuthorRole)
            };
        }

        return summaries;
    }

    private static CustomerNoteDTO MapNoteToDto(
        CustomerNoteRecord note,
        Profiles? authorProfile = null)
    {
        return new CustomerNoteDTO
        {
            Id = note.Id,
            CustomerId = note.CustomerId,
            AuthorUserId = note.AuthorUserId,
            AuthorName = ResolveAuthorName(authorProfile, note.AuthorName),
            AuthorRole = ResolveAuthorRole(authorProfile, note.AuthorRole),
            Body = note.Body,
            CreatedAt = note.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = note.UpdatedAt
        };
    }

    private static string ResolveAuthorName(
        Profiles? profile,
        string? storedAuthorName = null)
    {
        if (profile != null)
        {
            var fromProfile = ResolveAuthorNameFromProfile(profile);
            if (!string.IsNullOrWhiteSpace(fromProfile))
                return fromProfile;
        }

        if (!string.IsNullOrWhiteSpace(storedAuthorName))
            return storedAuthorName.Trim();

        return "Admin";
    }

    private static string ResolveAuthorNameFromProfile(Profiles profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.FullName))
        {
            var parts = profile.FullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
                return parts[0];

            return profile.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            var email = profile.Email.Trim();
            var atIndex = email.IndexOf('@');
            return atIndex > 0 ? email[..atIndex] : email;
        }

        return string.Empty;
    }

    private static string? ResolveAuthorRole(
        Profiles? profile,
        string? storedAuthorRole = null)
    {
        if (profile != null)
        {
            var fromProfile = FormatAuthorRole(profile.Role);
            if (fromProfile != null)
                return fromProfile;
        }

        if (!string.IsNullOrWhiteSpace(storedAuthorRole))
            return storedAuthorRole.Trim();

        return null;
    }

    private static string? FormatAuthorRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        var normalized = role.Trim().ToLowerInvariant();
        if (normalized is "user" or "")
            return null;

        return normalized switch
        {
            "admin" => "Admin",
            "support" => "Support",
            _ => char.ToUpper(normalized[0]) + normalized[1..]
        };
    }

    private static string? BuildPreview(string body)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length <= NotePreviewMaxLength)
            return trimmed;

        return trimmed[..NotePreviewMaxLength] + "…";
    }
}
