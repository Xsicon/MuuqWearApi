using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.DTO;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.OrderDTO;
using Supabase.Gotrue;
using System.Security.Claims;

namespace MuuqWear.Application.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize] //  all endpoints require JWT
public class OrderController : BaseController
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
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
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        // validate email 
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(Response<OrderDTO>.Fail("Email is required"));

        var response = await _orderService.PlaceOrder(userId, request);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    // =============================================
    // GET ORDER BY ID
    // GET api/Order/{orderId}
    // =============================================
    [HttpGet("{orderId}")]
    public async Task<ActionResult<Response<OrderDTO>>> GetOrder(Guid orderId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<OrderDTO>.Fail("Not authenticated"));

        if (orderId == Guid.Empty)
            return BadRequest(Response<OrderDTO>.Fail("Invalid order id"));

        var response = await _orderService.GetOrder(orderId, userId);
        if (!response.Success)
            return NotFound(response);

        return HandleResponse(response);
    }

    // =============================================
    // GET USER ORDERS
    // GET api/Order/my-orders
    // =============================================
    [HttpGet("my-orders")]
    public async Task<ActionResult<Response<List<OrderDTO>>>> GetUserOrders()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<List<OrderDTO>>.Fail("Not authenticated"));

        var response = await _orderService.GetUserOrders(userId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    // =============================================
    // GET ALL ORDERS (ADMIN)
    // GET api/Order/admin?status=&search=&page=&pageSize=
    // =============================================
    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<PaginatedResponse<OrderDTO>>>> GetAllOrders(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var response = await _orderService.GetAllOrders(
            status, search, page, pageSize);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET ORDER DETAIL (ADMIN)
    // GET api/Order/admin/{orderId}
    // =============================================
    [HttpGet("admin/{orderId}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<OrderDTO>>> GetOrderDetail(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return BadRequest(Response<OrderDTO>.Fail("Invalid order id"));

        var response = await _orderService.GetOrderDetail(orderId);
        if (!response.Success) return NotFound(response);
        return HandleResponse(response);
    }

    // =============================================
    // UPDATE ORDER STATUS (ADMIN)
    // PATCH api/Order/admin/{orderId}/status
    // =============================================
    [HttpPatch("admin/{orderId}/status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<OrderDTO>>> UpdateOrderStatus(
        Guid orderId,
        [FromBody] UpdateOrderStatusDTO request)
    {
        if (orderId == Guid.Empty)
            return BadRequest(Response<OrderDTO>.Fail("Invalid order id"));

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<OrderDTO>.Fail("Status is required"));

        var response = await _orderService.UpdateOrderStatus(
            orderId, request.Status);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // BULK UPDATE ORDER STATUS (ADMIN)
    // PATCH api/Order/admin/bulk-status
    // =============================================
    [HttpPatch("admin/bulk-status")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Response<int>>> BulkUpdateOrderStatus(
        [FromBody] BulkUpdateOrderStatusDTO request)
    {
        if (!request.OrderIds.Any())
            return BadRequest(Response<int>.Fail("No orders selected"));

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(Response<int>.Fail("Status is required"));

        var response = await _orderService
            .BulkUpdateOrderStatus(request.OrderIds, request.Status);

        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}