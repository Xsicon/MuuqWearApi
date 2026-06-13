using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.NotificationDTO;

namespace MuuqWear.Application.Interfaces;
public interface INotificationService
{
    Task<Response<List<NotificationDTO>>> GetRecent(DateTime? lastReadAt);
}
