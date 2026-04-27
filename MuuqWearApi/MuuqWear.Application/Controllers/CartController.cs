using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.CartDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // =============================================
    // HELPER — get user id from cookie claims ✅
    // =============================================
    private Guid? GetUserId()
    {
        // .NET maps "sub" to NameIdentifier ✅
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(sub)) return null;
        if (Guid.TryParse(sub, out var userId)) return userId;
        return null;
    }
    // =============================================
    // GET CART
    // GET api/Cart
    // =============================================
    [HttpGet]
    public async Task<ActionResult<Response<CartDTO>>> GetCart()
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var response = await _cartService.GetCart(userId.Value);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Response<CartDTO>>> AddItem(
        [FromBody] AddCartItemDTO request)
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        if (request.ProductId == Guid.Empty)
            return BadRequest(Response<CartDTO>.Fail("Invalid product"));

        if (string.IsNullOrWhiteSpace(request.Size))
            return BadRequest(Response<CartDTO>.Fail("Size is required"));

        if (request.Quantity < 1)
            return BadRequest(Response<CartDTO>.Fail("Quantity must be at least 1"));

        var response = await _cartService.AddItem(userId.Value, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Response<CartDTO>>> UpdateQuantity(
        [FromBody] UpdateCartItemDTO request)
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));
        if (request.CartItemId == Guid.Empty)
            return BadRequest(Response<CartDTO>.Fail("Invalid cart item"));

        if (request.Quantity < 1)
            return BadRequest(Response<CartDTO>.Fail("Quantity must be at least 1"));

        var response = await _cartService.UpdateQuantity(userId.Value, request);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpDelete("remove/{cartItemId}")]
    public async Task<ActionResult<Response<CartDTO>>> RemoveItem(Guid cartItemId)
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        if (cartItemId == Guid.Empty)
            return BadRequest(Response<CartDTO>.Fail("Invalid cart item"));

        var response = await _cartService.RemoveItem(userId.Value, cartItemId);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpDelete("clear")]
    public async Task<ActionResult<Response<CartDTO>>> ClearCart()
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var response = await _cartService.ClearCart(userId.Value);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("merge")]
    public async Task<ActionResult<Response<CartDTO>>> MergeCart(
        [FromBody] List<AddCartItemDTO> guestItems)
    {
        var userId = GetUserId();
        if (userId == null)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        if (guestItems == null || !guestItems.Any())
        {
            var cart = await _cartService.GetCart(userId.Value);
            return Ok(cart);
        }

        var response = await _cartService.MergeCart(userId.Value, guestItems);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
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
