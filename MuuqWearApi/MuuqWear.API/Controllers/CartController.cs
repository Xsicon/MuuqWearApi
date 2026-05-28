using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.CartDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : BaseController
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET CART
    // GET api/Cart
    // =============================================
    [HttpGet]
    public async Task<ActionResult<Response<CartDTO>>> GetCart()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var response = await _cartService.GetCart(userId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Response<CartDTO>>> AddItem(
        [FromBody] AddCartItemDTO request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        if (request.ProductId == Guid.Empty)
            return BadRequest(Response<CartDTO>.Fail("Invalid product"));

        if (string.IsNullOrWhiteSpace(request.Size))
            return BadRequest(Response<CartDTO>.Fail("Size is required"));

        if (request.Quantity < 1)
            return BadRequest(Response<CartDTO>.Fail("Quantity must be at least 1"));

        var response = await _cartService.AddItem(userId, request);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Response<CartDTO>>> UpdateQuantity(
        [FromBody] UpdateCartItemDTO request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));
        if (request.CartItemId == Guid.Empty)
            return BadRequest(Response<CartDTO>.Fail("Invalid cart item"));

        if (request.Quantity < 1)
            return BadRequest(Response<CartDTO>.Fail("Quantity must be at least 1"));

        var response = await _cartService.UpdateQuantity(userId, request);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    [HttpDelete("remove/{cartItemId}")]
    public async Task<ActionResult<Response<CartDTO>>> RemoveItem(Guid cartItemId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        if (cartItemId == Guid.Empty)
            return BadRequest(Response<CartDTO>.Fail("Invalid cart item"));

        var response = await _cartService.RemoveItem(userId, cartItemId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    [HttpDelete("clear")]
    public async Task<ActionResult<Response<CartDTO>>> ClearCart()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var response = await _cartService.ClearCart(userId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    [HttpPost("merge")]
    public async Task<ActionResult<Response<CartDTO>>> MergeCart(
        [FromBody] List<AddCartItemDTO> guestItems)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        if (guestItems == null || !guestItems.Any())
        {
            var cart = await _cartService.GetCart(userId);
            return Ok(cart);
        }

        var response = await _cartService.MergeCart(userId, guestItems);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    [HttpGet("test-auth")]
    [Authorize]
    public ActionResult TestAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            message = "authenticated",
            userId = sub,
            allClaims = claims
        });
    }
}
