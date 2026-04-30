using MuuqWear.API.Models;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.CartDTO;
using Supabase;

namespace MuuqWear.Application.Service;
public class CartService : ICartService
{
    private readonly Client _client;

    public CartService(Client client)
    {
        _client = client;
    }

    // =============================================
    // GET CART
    // =============================================
    public async Task<Response<CartDTO>> GetCart(Guid userId)
    {
        try
        {
            // single query with JOIN 
            var parameters = new Dictionary<string, object>
        {
            { "p_user_id", userId }
        };

            var result = await _client.Rpc("get_cart", parameters);

            // deserialize
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            };

            var items = System.Text.Json.JsonSerializer
                .Deserialize<List<CartItemDTO>>(
                    result.Content ?? "[]", options)
                ?? new List<CartItemDTO>();

            var cart = new CartDTO { Items = items };

            return Response<CartDTO>.SuccessResponse(cart, "Cart fetched");
        }
        catch (Exception ex)
        {
            return Response<CartDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // ADD ITEM
    // =============================================
    public async Task<Response<CartDTO>> AddItem(
        Guid userId, AddCartItemDTO request)
    {
        try
        {
            if (request.Quantity < 1)
                return Response<CartDTO>.Fail("Quantity must be at least 1");

            // check if product exists and has enough stock
            var product = await _client
                .From<Product>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.ProductId.ToString())
                .Single();

            if (product == null)
                return Response<CartDTO>.Fail("Product not found");

            // check if same product + size already in cart
            var existing = await _client
                .From<CartItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Filter("product_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.ProductId.ToString())
                .Filter("size",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.Size)
                .Single();

            if (existing != null)
            {
                // item exists → increase quantity
                var newQuantity = existing.Quantity + request.Quantity;

                // cap at stock level
                newQuantity = Math.Min(newQuantity, product.Stock);

                existing.Quantity = newQuantity;

                await _client
                    .From<CartItem>()
                    .Update(existing);
            }
            else
            {
                // new item → insert
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductId = request.ProductId,
                    Size = request.Size,
                    Quantity = Math.Min(request.Quantity, product.Stock),
                    CreatedAt = DateTime.UtcNow
                };

                await _client
                    .From<CartItem>()
                    .Insert(cartItem);
            }

            // return updated cart
            return await GetCart(userId);
        }
        catch (Exception ex)
        {
            return Response<CartDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // UPDATE QUANTITY
    // =============================================
    public async Task<Response<CartDTO>> UpdateQuantity(
        Guid userId, UpdateCartItemDTO request)
    {
        try
        {
            // validate quantity
            if (request.Quantity < 1)
                return Response<CartDTO>.Fail("Quantity must be at least 1");

            // fetch cart item
            var cartItem = await _client
                .From<CartItem>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    request.CartItemId.ToString())
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Single();

            if (cartItem == null)
                return Response<CartDTO>.Fail("Cart item not found");

            // check stock
            var product = await _client
                .From<Product>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    cartItem.ProductId.ToString())
                .Single();

            // cap at stock level
            var newQuantity = product != null
                ? Math.Min(request.Quantity, product.Stock)
                : request.Quantity;

            cartItem.Quantity = newQuantity;

            await _client
                .From<CartItem>()
                .Update(cartItem);

            return await GetCart(userId);
        }
        catch (Exception ex)
        {
            return Response<CartDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // REMOVE ITEM
    // =============================================
    public async Task<Response<CartDTO>> RemoveItem(
        Guid userId, Guid cartItemId)
    {
        try
        {
            // security check → only delete user's own items
            await _client
                .From<CartItem>()
                .Filter("id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    cartItemId.ToString())
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Delete();

            return await GetCart(userId);
        }
        catch (Exception ex)
        {
            return Response<CartDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // CLEAR CART
    // =============================================
    public async Task<Response<CartDTO>> ClearCart(Guid userId)
    {
        try
        {
            await _client
                .From<CartItem>()
                .Filter("user_id",
                    Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Delete();

            // return empty cart 
            return Response<CartDTO>.SuccessResponse(
                new CartDTO(), "Cart cleared");
        }
        catch (Exception ex)
        {
            return Response<CartDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // MERGE CART (guest → logged in)
    // =============================================
    public async Task<Response<CartDTO>> MergeCart(
        Guid userId, List<AddCartItemDTO> guestItems)
    {
        try
        {
            // add each guest item to DB cart
            // AddItem handles duplicates automatically
            foreach (var item in guestItems)
            {
                await AddItem(userId, item);
            }

            return await GetCart(userId);
        }
        catch (Exception ex)
        {
            return Response<CartDTO>.Fail("Error: " + ex.Message);
        }
    }

    // =============================================
    // PRIVATE HELPER — map CartItem to DTO
    // fetches product details for each item
    // =============================================
}
