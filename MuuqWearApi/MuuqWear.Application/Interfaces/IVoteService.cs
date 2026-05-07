using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.VoteDTO;

namespace MuuqWear.Application.Interfaces;

public interface IVoteService
{
    Task<Response<List<VoteItemDTO>>> GetActiveItems(Guid userId);

    Task<Response<List<VoteItemDTO>>> GetFinishedItems();
    Task<Response<VoteStatsDTO>> GetStats();
    Task<Response<VoteItemDTO>> CastVote(Guid userId, CastVoteDTO request);
    Task<Response<bool>> RegisterPreOrder(Guid userId, PreOrderDTO request);
}
