using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using Supabase.Gotrue;
using System.Security.Claims;

namespace MuuqWear.Application.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize] // ✅ all endpoints require JWT
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // get userId from JWT ✅
    private Guid? GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub)) return null;
        if (Guid.TryParse(sub, out var userId)) return userId;
        return null;
    }

    // =============================================
    // PLACE ORDER
    // POST api/Order/place
    // =============================================
    [HttpPost("place")]
    public async Task<ActionResult<Response<OrderDTO>>> PlaceOrder(
        [FromBody] PlaceOrderDTO request)
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        // validate email ✅
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<OrderDTO>.Fail("Email is required"));

        var response = await _orderService.PlaceOrder(userId.Value, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    // =============================================
    // GET ORDER BY ID
    // GET api/Order/{orderId}
    // =============================================
    [HttpGet("{orderId}")]
    public async Task<ActionResult<Response<OrderDTO>>> GetOrder(Guid orderId)
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        if (orderId == Guid.Empty)
            return BadRequest(Response<OrderDTO>.Fail("Invalid order id"));

        var response = await _orderService.GetOrder(orderId, userId.Value);
        if (!response.Success)
            return NotFound(response);

        return Ok(response);
    }

    // =============================================
    // GET USER ORDERS
    // GET api/Order/my-orders
    // =============================================
    [HttpGet("my-orders")]
    public async Task<ActionResult<Response<List<OrderDTO>>>> GetUserOrders()
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<List<OrderDTO>>.Fail("Not authenticated"));

        var response = await _orderService.GetUserOrders(userId.Value);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}