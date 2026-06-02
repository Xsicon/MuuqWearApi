using MuuqWear.API.Shared;
using MuuqWear.Model.DTO.Payment;

namespace MuuqWear.Application.Interfaces;

public interface IPaymentService
{
    Task<Response<CreatePaymentIntentResultDTO>> CreatePaymentIntent(
        Guid orderId, Guid userId);
    Task<Response<bool>> HandleWebhookEvent(Stripe.Event stripeEvent);
}
