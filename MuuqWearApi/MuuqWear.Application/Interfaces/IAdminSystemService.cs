using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Application.Interfaces;

public interface IAdminSystemService
{
    Task<Response<SystemHealthOverviewModel>> GetOverviewAsync();
    Task<Response<List<IntegrationStatusModel>>> GetIntegrationsAsync();
    Task<Response<IntegrationStatusModel>> TestIntegrationAsync(string name);
    Task<Response<IntegrationStatusModel>> ReconnectIntegrationAsync(string name);
    Task<Response<SyncJobResultModel>> RunSyncJobAsync(string jobKey);
    Task<Response<SystemLogsPageModel>> GetLogsAsync(
        int days, string level, string? search, int page, int pageSize);
    Task<Response<List<BackgroundJobModel>>> GetBackgroundJobsAsync();
    Task<Response<BackgroundJobModel>> GetBackgroundJobByIdAsync(Guid id);
    Task<Response<SyncJobResultModel>> GetSyncJobByIdAsync(Guid id);
}
