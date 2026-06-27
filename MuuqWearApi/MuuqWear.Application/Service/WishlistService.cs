using Microsoft.Extensions.Logging;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.WishlistDTO;
using MuuqWear.Model.Models.Product;
using MuuqWear.Model.Models.WishlistItem;

namespace MuuqWear.Application.Service;

public class WishlistService : IWishlistService
{
    private readonly Supabase.Client _client;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(SupabaseClientFactory factory, ILogger<WishlistService> logger)
    {
        _client = factory.CreateClient();
        _logger = logger;
    }

    // =============================================
    // GET WISHLIST
    // Queries the wishlist_items table directly and joins product
    // data in-process. Never throws for an empty wishlist — returns
    // a successful empty list instead.
    // =============================================
    public async Task<Response<List<WishlistItemDTO>>> GetWishlist(Guid userId)
    {
        try
        {
            var itemsResult = await _client
                .From<WishlistItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Order("created_at",
                    Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var wishlistItems = itemsResult?.Models ?? new List<WishlistItem>();

            if (wishlistItems.Count == 0)
                return Response<List<WishlistItemDTO>>.SuccessResponse(
                    new List<WishlistItemDTO>(), "Wishlist fetched");

            var productIds = wishlistItems
                .Select(i => (object)i.ProductId.ToString())
                .ToList();

            var productsResult = await _client
                .From<Product>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.In,
                    productIds)
                .Get();

            var products = (productsResult?.Models ?? new List<Product>())
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var dtos = new List<WishlistItemDTO>();
            foreach (var item in wishlistItems)
            {
                products.TryGetValue(item.ProductId, out var product);
                if (product == null || product.IsDeleted)
                    continue;

                dtos.Add(new WishlistItemDTO
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    ProductImageUrl = product.ImageUrl,
                    ProductPrice = product.Price,
                    AddedAt = item.CreatedAt
                });
            }

            return Response<List<WishlistItemDTO>>.SuccessResponse(dtos, "Wishlist fetched");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetWishlist failed for user {UserId}", userId);
            return Response<List<WishlistItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // ADD TO WISHLIST (idempotent)
    // =============================================
    public async Task<Response<List<WishlistItemDTO>>> AddToWishlist(Guid userId, Guid productId)
    {
        try
        {
            var product = await _client
                .From<Product>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    productId.ToString())
                .Single();

            if (product == null || product.IsDeleted)
                return Response<List<WishlistItemDTO>>.Fail("Product not found");

            var existing = await _client
                .From<WishlistItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Filter("product_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    productId.ToString())
                .Single();

            if (existing == null)
            {
                var wishlistItem = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };

                await _client
                    .From<WishlistItem>()
                    .Insert(wishlistItem);
            }

            return await GetWishlist(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddToWishlist failed for user {UserId}, product {ProductId}", userId, productId);
            return Response<List<WishlistItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // REMOVE FROM WISHLIST (idempotent)
    // =============================================
    public async Task<Response<List<WishlistItemDTO>>> RemoveFromWishlist(Guid userId, Guid productId)
    {
        try
        {
            await _client
                .From<WishlistItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Filter("product_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    productId.ToString())
                .Delete();

            return await GetWishlist(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveFromWishlist failed for user {UserId}, product {ProductId}", userId, productId);
            return Response<List<WishlistItemDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // MERGE WISHLIST (guest → logged in)
    // Adds all guest product ids, ignoring duplicates and invalid products.
    // =============================================
    public async Task<Response<List<WishlistItemDTO>>> MergeWishlist(Guid userId, List<Guid> productIds)
    {
        try
        {
            var distinctIds = productIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            foreach (var productId in distinctIds)
            {
                var product = await _client
                    .From<Product>()
                    .Filter("id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        productId.ToString())
                    .Single();

                if (product == null || product.IsDeleted)
                    continue;

                var existing = await _client
                    .From<WishlistItem>()
                    .Filter("user_id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        userId.ToString())
                    .Filter("product_id",
                        Supabase.Postgrest.Constants.Operator.Equals,
                        productId.ToString())
                    .Single();

                if (existing != null)
                    continue;

                var wishlistItem = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };

                await _client
                    .From<WishlistItem>()
                    .Insert(wishlistItem);
            }

            return await GetWishlist(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MergeWishlist failed for user {UserId}", userId);
            return Response<List<WishlistItemDTO>>.Fail("Error: " + ex.Message);
        }
    }
}
