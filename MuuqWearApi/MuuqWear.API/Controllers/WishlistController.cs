using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.WishlistDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : BaseController
{
    private readonly IWishlistService _wishlistService;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(
        IWishlistService wishlistService,
        ILogger<WishlistController> logger)
    {
        _wishlistService = wishlistService;
        _logger = logger;
    }

    // GET api/Wishlist
    [HttpGet]
    public async Task<ActionResult<Response<List<WishlistItemDTO>>>> GetWishlist()
    {
        var userId = GetUserId();
        _logger.LogInformation("GetWishlist called for user {UserId}", userId);

        if (userId == Guid.Empty)
            return StatusCode(401, Response<List<WishlistItemDTO>>.Fail("Not authenticated"));

        var response = await _wishlistService.GetWishlist(userId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    // POST api/Wishlist/add   body: { "ProductId": "<guid>" }
    [HttpPost("add")]
    public async Task<ActionResult<Response<List<WishlistItemDTO>>>> AddToWishlist(
        [FromBody] AddWishlistItemDTO request)
    {
        var userId = GetUserId();
        _logger.LogInformation(
            "AddToWishlist called for user {UserId}, product {ProductId}",
            userId, request?.ProductId);

        if (userId == Guid.Empty)
            return StatusCode(401, Response<List<WishlistItemDTO>>.Fail("Not authenticated"));

        if (request == null || request.ProductId == Guid.Empty)
            return BadRequest(Response<List<WishlistItemDTO>>.Fail("Invalid product"));

        var response = await _wishlistService.AddToWishlist(userId, request.ProductId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    // DELETE api/Wishlist/remove/{productId}
    [HttpDelete("remove/{productId}")]
    public async Task<ActionResult<Response<List<WishlistItemDTO>>>> RemoveFromWishlist(Guid productId)
    {
        var userId = GetUserId();
        _logger.LogInformation(
            "RemoveFromWishlist called for user {UserId}, product {ProductId}",
            userId, productId);

        if (userId == Guid.Empty)
            return StatusCode(401, Response<List<WishlistItemDTO>>.Fail("Not authenticated"));

        if (productId == Guid.Empty)
            return BadRequest(Response<List<WishlistItemDTO>>.Fail("Invalid product"));

        var response = await _wishlistService.RemoveFromWishlist(userId, productId);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }

    // POST api/Wishlist/merge   body: { "ProductIds": ["<guid>", ...] }
    [HttpPost("merge")]
    public async Task<ActionResult<Response<List<WishlistItemDTO>>>> MergeWishlist(
        [FromBody] MergeWishlistRequestDTO request)
    {
        var userId = GetUserId();
        _logger.LogInformation(
            "MergeWishlist called for user {UserId}, {Count} ids",
            userId, request?.ProductIds?.Count ?? 0);

        if (userId == Guid.Empty)
            return StatusCode(401, Response<List<WishlistItemDTO>>.Fail("Not authenticated"));

        if (request?.ProductIds == null || !request.ProductIds.Any())
        {
            var current = await _wishlistService.GetWishlist(userId);
            if (!current.Success)
                return BadRequest(current);
            return HandleResponse(current);
        }

        var response = await _wishlistService.MergeWishlist(userId, request.ProductIds);
        if (!response.Success)
            return BadRequest(response);

        return HandleResponse(response);
    }
}
