using Microsoft.Extensions.Configuration;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSystem;
using MuuqWear.Model.Models.AdminSystem;
using MuuqWear.Model.Models.AffiliateApplication;
using MuuqWear.Model.Models.Order;
using MuuqWear.Model.Models.Profiles;
using Stripe;
using System.Globalization;
using System.Net.Http.Headers;

namespace MuuqWear.Application.Service;

public class AdminSystemService : IAdminSystemService
{
    private const string LastBackupMetadataKey = "last_backup_at";

    private static readonly HashSet<string> SupportedIntegrations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "stripe", "supabase", "sendgrid", "cloudflare"
        };

    private static readonly HashSet<string> SupportedSyncJobs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "stripe-orders", "affiliate-commissions", "inventory-erp"
        };

    private readonly Supabase.Client _adminClient;
    private readonly IAdminSettingService _adminSettingService;
    private readonly IOrderService _orderService;
    private readonly IAffiliateService _affiliateService;
    private readonly IConfiguration _configuration;

    public AdminSystemService(
        SupabaseAdminClientFactory adminFactory,
        IAdminSettingService adminSettingService,
        IOrderService orderService,
        IAffiliateService affiliateService,
        IConfiguration configuration)
    {
        _adminClient = adminFactory.CreateClient();
        _adminSettingService = adminSettingService;
        _orderService = orderService;
        _affiliateService = affiliateService;
        _configuration = configuration;
    }

    public async Task<Response<SystemHealthOverviewModel>> GetOverviewAsync()
    {
        try
        {
            var supabaseHealth = await _adminSettingService.CheckSupabaseHealth();
            var stripeHealth = await _adminSettingService.CheckStripeHealth();

            var supabase = supabaseHealth.Data;
            var stripe = stripeHealth.Data;

            var lastBackupAt = await GetLastBackupAtAsync();
            int? activeUsers = null;
            try
            {
                activeUsers = await CountActiveUsersAsync();
            }
            catch
            {
                // Leave null when profiles query fails
            }

            var overview = new SystemHealthOverviewModel
            {
                DatabaseIsHealthy = supabase?.IsHealthy ?? false,
                DatabaseStatus = supabase?.IsHealthy == true
                    ? "Connected"
                    : supabase?.Status ?? "Unhealthy",
                StripeIsHealthy = stripe?.IsHealthy ?? false,
                StripeStatus = stripe?.IsHealthy == true
                    ? "Processing payments normally"
                    : stripe?.Status ?? "Unhealthy",
                SupabaseIsHealthy = supabase?.IsHealthy ?? false,
                SupabaseStatus = supabase?.IsHealthy == true
                    ? "Database reachable"
                    : supabase?.Status ?? "Unhealthy",
                LastBackupAt = lastBackupAt,
                LastBackupDisplay = FormatLastBackupDisplay(lastBackupAt),
                ActiveUsersCount = activeUsers,
                CheckedAt = DateTime.UtcNow
            };

            return Response<SystemHealthOverviewModel>.SuccessResponse(
                overview, "System overview fetched");
        }
        catch (Exception ex)
        {
            return Response<SystemHealthOverviewModel>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<List<IntegrationStatusModel>>> GetIntegrationsAsync()
    {
        try
        {
            var integrations = new List<IntegrationStatusModel>
            {
                await CheckStripeIntegrationAsync(),
                await CheckSupabaseIntegrationAsync(),
                await CheckSendGridIntegrationAsync(),
                await CheckCloudflareIntegrationAsync()
            };

            return Response<List<IntegrationStatusModel>>.SuccessResponse(
                integrations, "Integrations fetched");
        }
        catch (Exception ex)
        {
            return Response<List<IntegrationStatusModel>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<IntegrationStatusModel>> TestIntegrationAsync(string name)
    {
        if (!SupportedIntegrations.Contains(name))
            return Response<IntegrationStatusModel>.Fail($"Unknown integration: {name}");

        try
        {
            var status = await CheckIntegrationByNameAsync(name);
            await WriteLogAsync(
                status.IsHealthy ? "Info" : "Warning",
                $"{status.Name} health check: {status.StatusDetail}",
                status.Name);

            return Response<IntegrationStatusModel>.SuccessResponse(
                status, $"{status.Name} health check completed");
        }
        catch (Exception ex)
        {
            return Response<IntegrationStatusModel>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<IntegrationStatusModel>> ReconnectIntegrationAsync(string name)
    {
        if (!SupportedIntegrations.Contains(name))
            return Response<IntegrationStatusModel>.Fail($"Unknown integration: {name}");

        try
        {
            var status = await CheckIntegrationByNameAsync(name);
            if (!status.SupportsReconnect)
            {
                return Response<IntegrationStatusModel>.Fail(
                    $"{status.Name} does not support reconnect");
            }

            status.StatusDetail = status.IsHealthy
                ? "Connection re-validated successfully"
                : status.StatusDetail;

            await WriteLogAsync(
                status.IsHealthy ? "Info" : "Warning",
                $"{status.Name} reconnect: {status.StatusDetail}",
                status.Name);

            return Response<IntegrationStatusModel>.SuccessResponse(
                status, $"{status.Name} reconnect completed");
        }
        catch (Exception ex)
        {
            return Response<IntegrationStatusModel>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<SyncJobResultModel>> RunSyncJobAsync(string jobKey)
    {
        if (!SupportedSyncJobs.Contains(jobKey))
            return Response<SyncJobResultModel>.Fail($"Unknown sync job: {jobKey}");

        if (jobKey.Equals("inventory-erp", StringComparison.OrdinalIgnoreCase))
        {
            return Response<SyncJobResultModel>.Fail(
                "ERP integration is not configured. Inventory sync is not available yet.");
        }

        var syncJob = new SyncJob
        {
            Id = Guid.NewGuid(),
            JobKey = jobKey.ToLowerInvariant(),
            Status = "running",
            Message = "Sync started",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            await _adminClient.From<SyncJob>().Insert(syncJob);
            await UpdateBackgroundJobStatusAsync(jobKey, "running", "Sync in progress");

            int recordsAffected;
            string message;

            if (jobKey.Equals("stripe-orders", StringComparison.OrdinalIgnoreCase))
                (recordsAffected, message) = await SyncStripeOrdersAsync();
            else
                (recordsAffected, message) = await SyncAffiliateCommissionsAsync();

            syncJob.Status = "completed";
            syncJob.Message = message;
            syncJob.CompletedAt = DateTime.UtcNow;
            syncJob.RecordsAffected = recordsAffected;
            await _adminClient.From<SyncJob>().Update(syncJob);

            await UpdateBackgroundJobStatusAsync(
                jobKey, "completed", message, recordsAffected);

            await WriteLogAsync("Info", message, $"Sync:{jobKey}");

            return Response<SyncJobResultModel>.SuccessResponse(
                MapSyncJob(syncJob), message);
        }
        catch (Exception ex)
        {
            syncJob.Status = "failed";
            syncJob.Message = ex.Message;
            syncJob.CompletedAt = DateTime.UtcNow;
            try
            {
                await _adminClient.From<SyncJob>().Update(syncJob);
                await UpdateBackgroundJobStatusAsync(jobKey, "failed", ex.Message);
                await WriteLogAsync("Error", $"Sync {jobKey} failed: {ex.Message}", $"Sync:{jobKey}");
            }
            catch
            {
                // Best effort persistence
            }

            return Response<SyncJobResultModel>.Fail("Sync failed: " + ex.Message);
        }
    }

    public async Task<Response<SystemLogsPageModel>> GetLogsAsync(
        int days, string level, string? search, int page, int pageSize)
    {
        try
        {
            if (days < 1) days = 7;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var since = DateTime.UtcNow.AddDays(-days);
            var normalizedLevel = NormalizeLogLevel(level);

            // Server-side date + level filtering to avoid loading all logs.
            // Search stays client-side but is now bounded by `days`.
            var baseQuery = _adminClient
                .From<SystemLog>()
                .Order("logged_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Filter(
                    "logged_at",
                    Supabase.Postgrest.Constants.Operator.GreaterThan,
                    since.ToString("O", CultureInfo.InvariantCulture));

            if (normalizedLevel != null)
                baseQuery = baseQuery.Filter(
                    "level",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    normalizedLevel);

            var offset = (page - 1) * pageSize;

            List<SystemLog> pageRows;
            int totalCount;

            if (string.IsNullOrWhiteSpace(search))
            {
                try
                {
                    totalCount = await baseQuery.Count(
                        Supabase.Postgrest.Constants.CountType.Exact);
                }
                catch
                {
                    var fallback = await baseQuery.Get();
                    totalCount = fallback.Models.Count;
                }

                var pageResult = await baseQuery
                    .Range(offset, offset + pageSize - 1)
                    .Get();

                pageRows = pageResult.Models;
            }
            else
            {
                var allSince = await baseQuery.Get();
                var filtered = allSince.Models
                    .Where(l =>
                        l.Message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (l.Source?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

                totalCount = filtered.Count;
                pageRows = filtered
                    .Skip(offset)
                    .Take(pageSize)
                    .ToList();
            }

            var model = new SystemLogsPageModel
            {
                Items = pageRows.Select(MapLog).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Response<SystemLogsPageModel>.SuccessResponse(model, "Logs fetched");
        }
        catch (Exception ex)
        {
            return Response<SystemLogsPageModel>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<List<BackgroundJobModel>>> GetBackgroundJobsAsync()
    {
        try
        {
            var result = await _adminClient
                .From<BackgroundJob>()
                .Order("name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var jobs = result.Models.Select(MapBackgroundJob).ToList();
            return Response<List<BackgroundJobModel>>.SuccessResponse(
                jobs, "Background jobs fetched");
        }
        catch (Exception ex)
        {
            return Response<List<BackgroundJobModel>>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<BackgroundJobModel>> GetBackgroundJobByIdAsync(Guid id)
    {
        try
        {
            var result = await _adminClient
                .From<BackgroundJob>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            var job = result.Models.FirstOrDefault();
            if (job == null)
                return Response<BackgroundJobModel>.Fail("Background job not found");

            return Response<BackgroundJobModel>.SuccessResponse(
                MapBackgroundJob(job), "Background job fetched");
        }
        catch (Exception ex)
        {
            return Response<BackgroundJobModel>.Fail("Error: " + ex.Message);
        }
    }

    public async Task<Response<SyncJobResultModel>> GetSyncJobByIdAsync(Guid id)
    {
        try
        {
            var result = await _adminClient
                .From<SyncJob>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            var job = result.Models.FirstOrDefault();
            if (job == null)
                return Response<SyncJobResultModel>.Fail("Sync job not found");

            return Response<SyncJobResultModel>.SuccessResponse(
                MapSyncJob(job), "Sync job fetched");
        }
        catch (Exception ex)
        {
            return Response<SyncJobResultModel>.Fail("Error: " + ex.Message);
        }
    }

    private async Task<IntegrationStatusModel> CheckIntegrationByNameAsync(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "stripe" => await CheckStripeIntegrationAsync(),
            "supabase" => await CheckSupabaseIntegrationAsync(),
            "sendgrid" => await CheckSendGridIntegrationAsync(),
            "cloudflare" => await CheckCloudflareIntegrationAsync(),
            _ => throw new ArgumentException($"Unknown integration: {name}")
        };
    }

    private async Task<IntegrationStatusModel> CheckStripeIntegrationAsync()
    {
        var health = await _adminSettingService.CheckStripeHealth();
        var data = health.Data;
        return new IntegrationStatusModel
        {
            Name = "Stripe",
            IsHealthy = data?.IsHealthy ?? false,
            StatusDetail = data?.IsHealthy == true
                ? "Processing payments normally"
                : data?.Status ?? "Unhealthy",
            SupportsLiveHealthCheck = true,
            SupportsReconnect = true,
            LastCheckedAt = data?.CheckedAt ?? DateTime.UtcNow
        };
    }

    private async Task<IntegrationStatusModel> CheckSupabaseIntegrationAsync()
    {
        var health = await _adminSettingService.CheckSupabaseHealth();
        var data = health.Data;
        return new IntegrationStatusModel
        {
            Name = "Supabase",
            IsHealthy = data?.IsHealthy ?? false,
            StatusDetail = data?.IsHealthy == true
                ? "Database reachable"
                : data?.Status ?? "Unhealthy",
            SupportsLiveHealthCheck = true,
            SupportsReconnect = true,
            LastCheckedAt = data?.CheckedAt ?? DateTime.UtcNow
        };
    }

    private async Task<IntegrationStatusModel> CheckSendGridIntegrationAsync()
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new IntegrationStatusModel
            {
                Name = "SendGrid",
                IsHealthy = false,
                StatusDetail = "API key not configured (SendGrid:ApiKey)",
                SupportsLiveHealthCheck = true,
                SupportsReconnect = true,
                LastCheckedAt = DateTime.UtcNow
            };
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var request = new HttpRequestMessage(
                HttpMethod.Get, "https://api.sendgrid.com/v3/user/profile");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return new IntegrationStatusModel
                {
                    Name = "SendGrid",
                    IsHealthy = true,
                    StatusDetail = "API key verified",
                    SupportsLiveHealthCheck = true,
                    SupportsReconnect = true,
                    LastCheckedAt = DateTime.UtcNow
                };
            }

            return new IntegrationStatusModel
            {
                Name = "SendGrid",
                IsHealthy = false,
                StatusDetail = $"SendGrid API returned {(int)response.StatusCode}",
                SupportsLiveHealthCheck = true,
                SupportsReconnect = true,
                LastCheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new IntegrationStatusModel
            {
                Name = "SendGrid",
                IsHealthy = false,
                StatusDetail = $"SendGrid check failed: {ex.Message}",
                SupportsLiveHealthCheck = true,
                SupportsReconnect = true,
                LastCheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<IntegrationStatusModel> CheckCloudflareIntegrationAsync()
    {
        var apiToken = _configuration["Cloudflare:ApiToken"];
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return new IntegrationStatusModel
            {
                Name = "Cloudflare",
                IsHealthy = false,
                StatusDetail = "API token not configured (Cloudflare:ApiToken)",
                SupportsLiveHealthCheck = true,
                SupportsReconnect = true,
                LastCheckedAt = DateTime.UtcNow
            };
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.cloudflare.com/client/v4/user/tokens/verify");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiToken);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode &&
                body.Contains("\"status\":\"active\"", StringComparison.OrdinalIgnoreCase))
            {
                return new IntegrationStatusModel
                {
                    Name = "Cloudflare",
                    IsHealthy = true,
                    StatusDetail = "API token active",
                    SupportsLiveHealthCheck = true,
                    SupportsReconnect = true,
                    LastCheckedAt = DateTime.UtcNow
                };
            }

            return new IntegrationStatusModel
            {
                Name = "Cloudflare",
                IsHealthy = false,
                StatusDetail = $"Cloudflare token verification failed ({(int)response.StatusCode})",
                SupportsLiveHealthCheck = true,
                SupportsReconnect = true,
                LastCheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new IntegrationStatusModel
            {
                Name = "Cloudflare",
                IsHealthy = false,
                StatusDetail = $"Cloudflare check failed: {ex.Message}",
                SupportsLiveHealthCheck = true,
                SupportsReconnect = true,
                LastCheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<(int RecordsAffected, string Message)> SyncStripeOrdersAsync()
    {
        var ordersResult = await _adminClient
            .From<Order>()
            .Filter("payment_status",
                Supabase.Postgrest.Constants.Operator.NotEqual, "paid")
            .Get();

        var candidates = ordersResult.Models
            .Where(o => !string.IsNullOrWhiteSpace(o.StripePaymentIntentId))
            .ToList();

        var paymentIntentService = new PaymentIntentService();
        var synced = 0;

        foreach (var order in candidates)
        {
            var intent = await paymentIntentService.GetAsync(order.StripePaymentIntentId!);
            if (!string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                continue;

            var finalize = await _orderService.FinalizeOrder(order.Id);
            if (finalize.Success)
                synced++;
        }

        var message = synced == 0
            ? "No unpaid orders required Stripe reconciliation"
            : $"Reconciled {synced} order(s) from Stripe";

        return (synced, message);
    }

    private async Task<(int RecordsAffected, string Message)> SyncAffiliateCommissionsAsync()
    {
        var ordersResult = await _adminClient
            .From<Order>()
            .Filter("payment_status",
                Supabase.Postgrest.Constants.Operator.Equals, "paid")
            .Get();

        var referralsResult = await _adminClient.From<AffiliateReferral>().Get();
        var referredOrderIds = referralsResult.Models
            .Select(r => r.OrderId)
            .ToHashSet();

        var pending = ordersResult.Models
            .Where(o => !string.IsNullOrWhiteSpace(o.PendingAffiliateCode))
            .Where(o => !referredOrderIds.Contains(o.Id))
            .ToList();

        var processed = 0;
        foreach (var order in pending)
        {
            var track = await _affiliateService.TrackOrderReferral(
                order.Id,
                order.UserId,
                order.Total,
                order.PendingAffiliateCode!);

            if (track.Success)
                processed++;
        }

        var message = processed == 0
            ? "No pending affiliate commissions to recalculate"
            : $"Recalculated commissions for {processed} order(s)";

        return (processed, message);
    }

    private async Task<DateTime?> GetLastBackupAtAsync()
    {
        try
        {
            var result = await _adminClient
                .From<SystemMetadata>()
                .Filter("key", Supabase.Postgrest.Constants.Operator.Equals, LastBackupMetadataKey)
                .Get();

            var row = result.Models.FirstOrDefault();
            if (row?.Value != null &&
                DateTime.TryParse(row.Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed.ToUniversalTime();
            }

            var backupJob = await _adminClient
                .From<BackgroundJob>()
                .Filter("job_key", Supabase.Postgrest.Constants.Operator.Equals, "daily-backup")
                .Get();

            return backupJob.Models.FirstOrDefault()?.LastRunAt;
        }
        catch
        {
            return null;
        }
    }

    private async Task<int> CountActiveUsersAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30);
        var result = await _adminClient
            .From<Profiles>()
            .Filter("is_deleted", Supabase.Postgrest.Constants.Operator.Equals, "false")
            .Get();

        return result.Models.Count(p =>
            p.LastActiveAt.HasValue && p.LastActiveAt.Value.ToUniversalTime() >= cutoff);
    }

    private async Task UpdateBackgroundJobStatusAsync(
        string jobKey,
        string status,
        string message,
        int? recordsAffected = null)
    {
        var nowUtc = DateTime.UtcNow;
        var result = await _adminClient
            .From<BackgroundJob>()
            .Filter("job_key", Supabase.Postgrest.Constants.Operator.Equals, jobKey.ToLowerInvariant())
            .Get();

        var job = result.Models.FirstOrDefault();
        if (job == null)
            return;

        job.Status = status;
        job.LastRunAt = nowUtc;
        job.LastRunMessage = recordsAffected.HasValue
            ? $"{message} ({recordsAffected} records)"
            : message;

        if (status is "completed" or "failed")
        {
            job.NextRunAt = CalculateNextRunAtUtc(jobKey, nowUtc);

            if (jobKey.Equals("daily-backup", StringComparison.OrdinalIgnoreCase) &&
                status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            {
                await UpsertMetadataAsync(
                    LastBackupMetadataKey,
                    nowUtc.ToString("O", CultureInfo.InvariantCulture));
            }
        }

        await _adminClient.From<BackgroundJob>().Update(job);
    }

    private static DateTime? CalculateNextRunAtUtc(string jobKey, DateTime nowUtc)
    {
        var key = jobKey.ToLowerInvariant();

        DateTime TodayAt(int hourUtc)
            => new DateTime(
                nowUtc.Year, nowUtc.Month, nowUtc.Day,
                hourUtc, 0, 0,
                DateTimeKind.Utc);

        switch (key)
        {
            case "daily-backup":
                {
                    var candidate = TodayAt(2);
                    return nowUtc < candidate ? candidate : candidate.AddDays(1);
                }

            case "inventory-erp":
                {
                    var candidate = TodayAt(3);
                    return nowUtc < candidate ? candidate : candidate.AddDays(1);
                }

            case "stripe-orders":
                {
                    var hours = new[] { 0, 6, 12, 18 };
                    DateTime? next = null;

                    foreach (var h in hours)
                    {
                        var candidate = TodayAt(h);
                        if (candidate > nowUtc)
                        {
                            next = next == null ? candidate : (candidate < next ? candidate : next);
                        }
                    }

                    return next ?? TodayAt(0).AddDays(1);
                }

            case "affiliate-commissions":
                {
                    var daysUntilSunday =
                        ((int)DayOfWeek.Sunday - (int)nowUtc.DayOfWeek + 7) % 7;
                    var candidateDate = nowUtc.Date.AddDays(daysUntilSunday);
                    var candidate = new DateTime(
                        candidateDate.Year, candidateDate.Month, candidateDate.Day,
                        4, 0, 0,
                        DateTimeKind.Utc);

                    return candidate <= nowUtc ? candidate.AddDays(7) : candidate;
                }

            default:
                return null;
        }
    }

    private async Task UpsertMetadataAsync(string key, string value)
    {
        var existing = await _adminClient
            .From<SystemMetadata>()
            .Filter("key", Supabase.Postgrest.Constants.Operator.Equals, key)
            .Get();

        var row = existing.Models.FirstOrDefault();
        if (row != null)
        {
            row.Value = value;
            row.UpdatedAt = DateTime.UtcNow;
            await _adminClient.From<SystemMetadata>().Update(row);
        }
        else
        {
            await _adminClient.From<SystemMetadata>().Insert(new SystemMetadata
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task WriteLogAsync(string level, string message, string? source)
    {
        try
        {
            await _adminClient.From<SystemLog>().Insert(new SystemLog
            {
                Id = Guid.NewGuid(),
                LoggedAt = DateTime.UtcNow,
                Level = level,
                Message = message,
                Source = source
            });
        }
        catch
        {
            // Logging must not break admin operations
        }
    }

    private static string? NormalizeLogLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level) ||
            level.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;

        return level.ToLowerInvariant() switch
        {
            "error" => "Error",
            "warning" => "Warning",
            "info" => "Info",
            _ => null
        };
    }

    private static string FormatLastBackupDisplay(DateTime? utc)
    {
        if (!utc.HasValue)
            return "Unknown";

        var local = utc.Value.ToLocalTime();
        var today = DateTime.Today;
        var time = local.ToString("h:mm tt", CultureInfo.InvariantCulture);

        if (local.Date == today)
            return $"Today {time}";
        if (local.Date == today.AddDays(-1))
            return $"Yesterday {time}";

        return local.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
    }

    private static SystemLogEntryModel MapLog(SystemLog log) => new()
    {
        Id = log.Id,
        Timestamp = log.LoggedAt,
        Level = log.Level,
        Message = log.Message,
        Source = log.Source
    };

    private static BackgroundJobModel MapBackgroundJob(BackgroundJob job) => new()
    {
        Id = job.Id,
        Name = job.Name,
        Schedule = job.Schedule,
        Status = job.Status,
        LastRunAt = job.LastRunAt,
        NextRunAt = job.NextRunAt,
        LastRunMessage = job.LastRunMessage
    };

    private static SyncJobResultModel MapSyncJob(SyncJob job) => new()
    {
        Id = job.Id,
        JobKey = job.JobKey,
        Status = job.Status,
        Message = job.Message,
        StartedAt = job.StartedAt,
        CompletedAt = job.CompletedAt,
        RecordsAffected = job.RecordsAffected
    };
}
