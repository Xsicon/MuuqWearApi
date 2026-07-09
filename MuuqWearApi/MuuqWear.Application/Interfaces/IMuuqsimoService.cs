using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.MuuqsimoDTO;

namespace MuuqWear.Application.Interfaces;

public interface IMuuqsimoService
{
    Task<Response<MuuqsimoPageDTO>> GetPublishedPageAsync(string? slug = null);
    Task<Response<List<MuuqsimoEventSummaryDTO>>> GetPublishedEventsAsync();
}
