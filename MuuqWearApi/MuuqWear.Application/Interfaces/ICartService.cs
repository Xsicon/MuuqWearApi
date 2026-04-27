using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.CartDTO;

namespace MuuqWear.Application.Interfaces;

public interface ICartService
{
    // get full cart for user
    Task<Response<CartDTO>> GetCart(Guid userId);

    // add item to cart
    // if same product + size exists → increase quantity
    Task<Response<CartDTO>> AddItem(Guid userId, AddCartItemDTO request);

    // update quantity of existing item
    Task<Response<CartDTO>> UpdateQuantity(Guid userId, UpdateCartItemDTO request);

    // remove single item from cart
    Task<Response<CartDTO>> RemoveItem(Guid userId, Guid cartItemId);

    // clear entire cart
    Task<Response<CartDTO>> ClearCart(Guid userId);

    // merge guest cookie cart into DB cart on login
    Task<Response<CartDTO>> MergeCart(Guid userId, List<AddCartItemDTO> guestItems);
}
