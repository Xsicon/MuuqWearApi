using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Interfaces;
using MuuqWear.API.Shared;
using MuuqWear.Model.Address;
using MuuqWear.Model.DTO.CartDTO;
using System.Security.Claims;

namespace MuuqWear.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    // ─── helper — read userId from JWT claims ─────────────────
    private Guid? GetUserId()
    {
        // .NET maps "sub" to NameIdentifier 
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(sub)) return null;
        if (Guid.TryParse(sub, out var userId)) return userId;
        return null;
    }

    // ─── GET ALL ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var result = await _addressService.GetUserAddresses(userId!.Value);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ─── GET SINGLE ───────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var result = await _addressService.GetAddress(id, userId!.Value);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ─── CREATE ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<AddressDTO>.Fail("Full name is required"));

        if (string.IsNullOrWhiteSpace(request.Street1))
            return BadRequest(Response<AddressDTO>.Fail("Street address is required"));

        if (string.IsNullOrWhiteSpace(request.City))
            return BadRequest(Response<AddressDTO>.Fail("City is required"));

        if (string.IsNullOrWhiteSpace(request.PostalCode))
            return BadRequest(Response<AddressDTO>.Fail("Postal code is required"));

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var result = await _addressService.CreateAddress(userId!.Value, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ─── UPDATE ───────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAddressDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(Response<AddressDTO>.Fail("Full name is required"));

        if (string.IsNullOrWhiteSpace(request.Street1))
            return BadRequest(Response<AddressDTO>.Fail("Street address is required"));

        if (string.IsNullOrWhiteSpace(request.City))
            return BadRequest(Response<AddressDTO>.Fail("City is required"));

        if (string.IsNullOrWhiteSpace(request.PostalCode))
            return BadRequest(Response<AddressDTO>.Fail("Postal code is required"));

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var result = await _addressService.UpdateAddress(id, userId!.Value, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ─── DELETE ───────────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var result = await _addressService.DeleteAddress(id, userId!.Value);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ─── SET DEFAULT ──────────────────────────────────────────
    [HttpPatch("{id}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401, Response<CartDTO>.Fail("Not authenticated"));

        var result = await _addressService.SetDefault(id, userId!.Value);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}