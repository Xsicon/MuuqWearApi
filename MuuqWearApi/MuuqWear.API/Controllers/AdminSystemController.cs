using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Application.Shared;
using MuuqWear.Model.DTO.AdminSystem;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AdminAuthorizationPolicies.AdminSystem)]
public class AdminSystemController : BaseController
{
    private readonly IAdminSystemService _adminSystemService;

    public AdminSystemController(IAdminSystemService adminSystemService)
    {
        _adminSystemService = adminSystemService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<Response<SystemHealthOverviewModel>>> GetOverview()
    {
        var response = await _adminSystemService.GetOverviewAsync();
        return HandleResponse(response);
    }

    [HttpGet("integrations")]
    public async Task<ActionResult<Response<List<IntegrationStatusModel>>>> GetIntegrations()
    {
        var response = await _adminSystemService.GetIntegrationsAsync();
        return HandleResponse(response);
    }

    [HttpPost("integrations/{name}/test")]
    public async Task<ActionResult<Response<IntegrationStatusModel>>> TestIntegration(string name)
    {
        var response = await _adminSystemService.TestIntegrationAsync(name);
        return HandleResponse(response);
    }

    [HttpPost("integrations/{name}/reconnect")]
    public async Task<ActionResult<Response<IntegrationStatusModel>>> ReconnectIntegration(string name)
    {
        var response = await _adminSystemService.ReconnectIntegrationAsync(name);
        return HandleResponse(response);
    }

    [HttpPost("sync/{jobKey}")]
    public async Task<ActionResult<Response<SyncJobResultModel>>> RunSync(string jobKey)
    {
        if (jobKey.Equals("inventory-erp", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(501, Response<SyncJobResultModel>.Fail(
                "ERP integration is not configured. Inventory sync is not available yet."));
        }

        var response = await _adminSystemService.RunSyncJobAsync(jobKey);
        return HandleResponse(response);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<Response<SystemLogsPageModel>>> GetLogs(
        [FromQuery] int days = 7,
        [FromQuery] string level = "all",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var response = await _adminSystemService.GetLogsAsync(
            days, level, search, page, pageSize);
        return HandleResponse(response);
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<Response<List<BackgroundJobModel>>>> GetJobs()
    {
        var response = await _adminSystemService.GetBackgroundJobsAsync();
        return HandleResponse(response);
    }

    [HttpGet("jobs/{id:guid}")]
    public async Task<ActionResult<Response<BackgroundJobModel>>> GetJobById(Guid id)
    {
        var response = await _adminSystemService.GetBackgroundJobByIdAsync(id);
        return HandleResponse(response);
    }

    [HttpGet("sync/{id:guid}")]
    public async Task<ActionResult<Response<SyncJobResultModel>>> GetSyncJobById(Guid id)
    {
        var response = await _adminSystemService.GetSyncJobByIdAsync(id);
        return HandleResponse(response);
    }
}
