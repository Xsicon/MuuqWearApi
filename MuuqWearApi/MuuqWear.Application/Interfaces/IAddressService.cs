using MuuqWear.API.Shared;
using MuuqWear.Model.Address;

namespace MuuqWear.API.Interfaces;

public interface IAddressService
{
    // get all addresses for a user
    Task<Response<List<AddressDTO>>> GetUserAddresses(Guid userId);

    // get single address
    Task<Response<AddressDTO>> GetAddress(Guid addressId, Guid userId);

    // create new address
    Task<Response<AddressDTO>> CreateAddress(Guid userId, CreateAddressDTO request);

    // update existing address
    Task<Response<AddressDTO>> UpdateAddress(Guid addressId, Guid userId, UpdateAddressDTO request);

    // delete address
    Task<Response<bool>> DeleteAddress(Guid addressId, Guid userId);

    // set address as default
    Task<Response<AddressDTO>> SetDefault(Guid addressId, Guid userId);
}