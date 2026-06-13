using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.VoteDTO;

namespace MuuqWear.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoteController : BaseController
{
    private readonly IVoteService _voteService;

    public VoteController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    // =============================================
    // GET ACTIVE ITEMS
    // GET api/Vote/active
    //  requires login — to check HasVoted per user
    // =============================================
    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<Response<List<VoteItemDTO>>>> GetActiveItems()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401,
                Response<List<VoteItemDTO>>.Fail("Not authenticated"));

        var response = await _voteService.GetActiveItems(userId);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET FINISHED ITEMS
    // GET api/Vote/finished
    //  public — no login needed to see finished items
    // =============================================
    [HttpGet("finished")]
    public async Task<ActionResult<Response<List<VoteItemDTO>>>> GetFinishedItems()
    {
        var response = await _voteService.GetFinishedItems();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // GET STATS
    // GET api/Vote/stats
    //  public — stats visible to everyone
    // =============================================
    [HttpGet("stats")]
    public async Task<ActionResult<Response<VoteStatsDTO>>> GetStats()
    {
        var response = await _voteService.GetStats();
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // CAST VOTE
    // POST api/Vote/cast
    //  requires login
    // =============================================
    [HttpPost("cast")]
    [Authorize]
    public async Task<ActionResult<Response<VoteItemDTO>>> CastVote(
        [FromBody] CastVoteDTO request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401,
                Response<VoteItemDTO>.Fail("Not authenticated"));

        if (request.VoteItemId == Guid.Empty)
            return BadRequest(Response<VoteItemDTO>
                .Fail("Invalid vote item id"));

        var response = await _voteService.CastVote(userId, request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // =============================================
    // REGISTER PRE-ORDER
    // POST api/Vote/pre-order
    //  requires login
    // =============================================
    [HttpPost("pre-order")]
    [Authorize]
    public async Task<ActionResult<Response<bool>>> RegisterPreOrder(
        [FromBody] PreOrderDTO request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return StatusCode(401,
                Response<bool>.Fail("Not authenticated"));

        if (request.VoteItemId == Guid.Empty)
            return BadRequest(Response<bool>
                .Fail("Invalid vote item id"));

        var response = await _voteService.RegisterPreOrder(userId, request);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }

    // GET ACTIVE ITEMS (PUBLIC)
    // GET api/Vote/active/public
    //  no login needed — HasVoted = false for all
    // =============================================
    [HttpGet("active/public")]
    public async Task<ActionResult<Response<List<VoteItemDTO>>>> GetActiveItemsPublic()
    {
        //  pass Guid.Empty → service skips user vote checks
        var response = await _voteService.GetActiveItems(Guid.Empty);
        if (!response.Success) return BadRequest(response);
        return HandleResponse(response);
    }
}
