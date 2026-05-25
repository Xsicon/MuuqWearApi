using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Service;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.NotificationDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class NotificationController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly IProfileService _profileService;

    public NotificationController(
        INotificationService notificationService,IProfileService profileService)
    {
        _notificationService = notificationService;
        _profileService = profileService;
    }

    // GET api/Notification/recent
    [HttpGet("recent")]
    public async Task<ActionResult<Response<List<NotificationDTO>>>> GetRecent()
    {
        var userId = GetUserId();

        //  get lastReadAt from profile
        DateTime? lastReadAt = null;

        if (userId != Guid.Empty)
        {
            var profile = await _profileService.GetProfile(userId);
            if (profile.Success && profile.Data != null)
                lastReadAt = profile.Data.NotificationsReadAt;
        }

        var response = await _notificationService.GetRecent(lastReadAt);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}
