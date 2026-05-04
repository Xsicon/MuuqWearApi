using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using System.Security.Claims;

namespace MuuqWear.Application.Controllers;

public class BaseController : ControllerBase
{
    // ✅ reusable response handler — all controllers inherit this
    protected ActionResult<Response<T>> HandleResponse<T>(Response<T> result)
    {
        if (!result.Success && result.Message.Contains("JWT_EXPIRED"))
            return StatusCode(401, result);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    // ✅ reusable userId reader — no more copy paste in every controller
    protected Guid GetUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub)) return Guid.Empty;
        if (Guid.TryParse(sub, out var userId)) return userId;
        return Guid.Empty;
    }
}
