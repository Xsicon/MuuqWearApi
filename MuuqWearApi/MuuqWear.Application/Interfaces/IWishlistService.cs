using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.WishlistDTO;

namespace MuuqWear.Application.Interfaces;

public interface IWishlistService
{
    Task<Response<List<WishlistItemDTO>>> GetWishlist(Guid userId);

    Task<Response<List<WishlistItemDTO>>> AddToWishlist(Guid userId, Guid productId);

    Task<Response<List<WishlistItemDTO>>> RemoveFromWishlist(Guid userId, Guid productId);

    Task<Response<List<WishlistItemDTO>>> MergeWishlist(Guid userId, List<Guid> productIds);
}
