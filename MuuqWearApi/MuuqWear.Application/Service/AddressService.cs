using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using MuuqWear.Model.Address;
using MuuqWear.Model.Models;
using Supabase;

namespace MuuqWear.API.Service;

public class AddressService : IAddressService
{
    private readonly Supabase.Client _client;

    public AddressService(SupabaseClientFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── GET ALL ───────────────────────────────────────────────
    public async Task<Response<List<AddressDTO>>> GetUserAddresses(Guid userId)
    {
        try
        {
            var result = await _client
                .From<Address>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var data = result.Models.Select(ToDTO).ToList();
            return Response<List<AddressDTO>>.SuccessResponse(data, "Addresses fetched");
        }
        catch (Exception ex)
        {
            return Response<List<AddressDTO>>.Fail("Error: " + ex.Message);
        }
    }

    // ─── GET SINGLE ────────────────────────────────────────────
    public async Task<Response<AddressDTO>> GetAddress(Guid addressId, Guid userId)
    {
        try
        {

            var result = await _client
                .From<Address>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    addressId.ToString())
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Single();

            if (result == null)
                return Response<AddressDTO>.Fail("Address not found");

            return Response<AddressDTO>.SuccessResponse(ToDTO(result), "Address fetched");
        }
        catch (Exception ex)
        {
            return Response<AddressDTO>.Fail("Error: " + ex.Message);
        }
    }

    // ─── CREATE ────────────────────────────────────────────────
    public async Task<Response<AddressDTO>> CreateAddress(
        Guid userId, CreateAddressDTO request)
    {
        try
        {
       
            // if new address is default → unset all others first
            if (request.IsDefault)
                await UnsetAllDefaults(_client, userId);

            var address = new Address
            {
                UserId = userId,
                Label = request.Label,
                FullName = request.FullName,
                Street1 = request.Street1,
                Street2 = request.Street2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country,
                IsDefault = request.IsDefault
            };

            var result = await _client
                .From<Address>()
                .Insert(address);

            var created = result.Models.FirstOrDefault();
            if (created == null)
                return Response<AddressDTO>.Fail("Failed to create address");

            return Response<AddressDTO>.SuccessResponse(
                ToDTO(created), "Address created");
        }
        catch (Exception ex)
        {
            return Response<AddressDTO>.Fail("Error: " + ex.Message);
        }
    }

    // ─── UPDATE ────────────────────────────────────────────────
    public async Task<Response<AddressDTO>> UpdateAddress(
        Guid addressId, Guid userId, UpdateAddressDTO request)
    {
        try
        {
            

            // if updating to default → unset all others first
            if (request.IsDefault)
                await UnsetAllDefaults(_client, userId);

            var result = await _client
                .From<Address>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    addressId.ToString())
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Set(a => a.Label, request.Label)
                .Set(a => a.FullName, request.FullName)
                .Set(a => a.Street1, request.Street1)
                .Set(a => a.Street2!, request.Street2)
                .Set(a => a.City, request.City)
                .Set(a => a.State!, request.State)
                .Set(a => a.PostalCode, request.PostalCode)
                .Set(a => a.Country, request.Country)
                .Set(a => a.IsDefault, request.IsDefault)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<AddressDTO>.Fail("Failed to update address");

            return Response<AddressDTO>.SuccessResponse(
                ToDTO(updated), "Address updated");
        }
        catch (Exception ex)
        {
            return Response<AddressDTO>.Fail("Error: " + ex.Message);
        }
    }

    // ─── DELETE ────────────────────────────────────────────────
    public async Task<Response<bool>> DeleteAddress(Guid addressId, Guid userId)
    {
        try
        {
            await _client
                .From<Address>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    addressId.ToString())
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Delete();

            return Response<bool>.SuccessResponse(true, "Address deleted");
        }
        catch (Exception ex)
        {
            return Response<bool>.Fail("Error: " + ex.Message);
        }
    }

    // ─── SET DEFAULT ───────────────────────────────────────────
    public async Task<Response<AddressDTO>> SetDefault(Guid addressId, Guid userId)
    {
        try
        {

            // unset all first
            await UnsetAllDefaults(_client, userId);

            // set this one as default
            var result = await _client
                .From<Address>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals,
                    addressId.ToString())
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals,
                    userId.ToString())
                .Set(a => a.IsDefault, true)
                .Update();

            var updated = result.Models.FirstOrDefault();
            if (updated == null)
                return Response<AddressDTO>.Fail("Address not found");

            return Response<AddressDTO>.SuccessResponse(
                ToDTO(updated), "Default address updated");
        }
        catch (Exception ex)
        {
            return Response<AddressDTO>.Fail("Error: " + ex.Message);
        }
    }

    // ─── HELPERS ───────────────────────────────────────────────

    // unset all defaults for a user — called before setting a new default
    private async Task UnsetAllDefaults(Client client, Guid userId)
    {
        await client
            .From<Address>()
            .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals,
                userId.ToString())
            .Set(a => a.IsDefault, false)
            .Update();
    }

    // map Address → AddressDTO
    private AddressDTO ToDTO(Address a) => new()
    {
        Id = a.Id,
        Label = a.Label,
        FullName = a.FullName,
        Street1 = a.Street1,
        Street2 = a.Street2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        IsDefault = a.IsDefault,
        CreatedAt = a.CreatedAt
    };
}