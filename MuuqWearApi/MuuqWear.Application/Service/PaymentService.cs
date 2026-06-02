using Microsoft.Extensions.Configuration;
using MuuqWear.API.Shared;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.Payment;
using MuuqWear.Model.Models.Order;
using Stripe;
using Stripe.Climate;
using Supabase;

namespace MuuqWear.Application.Service;

public class PaymentService : IPaymentService
{
    private readonly Client _client;
    private readonly IConfiguration _config;
    private readonly IOrderService _orderService;

    public PaymentService(SupabaseClientFactory factory, IConfiguration config, IOrderService orderService)
    {
        _client = factory.CreateClient();
        _config = config;
        _orderService = orderService;
    }

    public async Task<Response<CreatePaymentIntentResultDTO>> CreatePaymentIntent(
        Guid orderId, Guid userId)
    {
        try
        {
            // 1. Load the order, verifying ownership
            var order = await _client.From<Model.Models.Order.Order>()
                .Where(o => o.Id == orderId).Single();

            if (order == null)
                return Response<CreatePaymentIntentResultDTO>.Fail("Order not found");

            if (order.UserId != userId)
                return Response<CreatePaymentIntentResultDTO>.Fail("Not your order");

            if (order.PaymentStatus == "paid")
                return Response<CreatePaymentIntentResultDTO>.Fail(
                    "Order is already paid");

            // 2. Idempotency — if intent already exists, return it
            if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                var existing = await new PaymentIntentService()
                    .GetAsync(order.StripePaymentIntentId);

                return Response<CreatePaymentIntentResultDTO>.SuccessResponse(
                    new CreatePaymentIntentResultDTO
                    {
                        ClientSecret = existing.ClientSecret,
                        PublishableKey = _config["Stripe:PublishableKey"]!,
                        OrderId = order.Id,
                        Amount = order.Total
                    },
                    "Reusing existing payment intent");
            }

            // 3. Create new PaymentIntent
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(order.Total * 100),       // Stripe uses cents
                Currency = "usd",
                AutomaticPaymentMethods = new()
                {
                    Enabled = true
                },
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", order.Id.ToString() },
                    { "order_number", order.OrderNumber },
                    { "user_id", order.UserId.ToString() }
                }
            };

            var intent = await new PaymentIntentService().CreateAsync(options);

            // 4. Save intent id on the order
            order.StripePaymentIntentId = intent.Id;
            await _client.From<Model.Models.Order.Order>().Update(order);

            return Response<CreatePaymentIntentResultDTO>.SuccessResponse(
                new CreatePaymentIntentResultDTO
                {
                    ClientSecret = intent.ClientSecret,
                    PublishableKey = _config["Stripe:PublishableKey"]!,
                    OrderId = order.Id,
                    Amount = order.Total
                },
                "Payment intent created");
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[Payment] Stripe error: {ex.Message}");
            return Response<CreatePaymentIntentResultDTO>.Fail(
                $"Payment processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Payment] CreatePaymentIntent error: {ex.Message}");
            return Response<CreatePaymentIntentResultDTO>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Response<bool>> HandleWebhookEvent(Stripe.Event stripeEvent)
    {
        // Only the event types we care about
        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                return await HandlePaymentSucceeded(stripeEvent);

            case "payment_intent.payment_failed":
                return await HandlePaymentFailed(stripeEvent);

            default:
                // Many other event types exist; we ignore them silently
                Console.WriteLine($"[Webhook] Ignoring event type: {stripeEvent.Type}");
                return Response<bool>.SuccessResponse(true, "Event ignored");
        }
    }

    private async Task<Response<bool>> HandlePaymentSucceeded(Stripe.Event stripeEvent)
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent == null)
            return Response<bool>.Fail("Event payload missing PaymentIntent");

        if (!intent.Metadata.TryGetValue("order_id", out var orderIdStr) ||
            !Guid.TryParse(orderIdStr, out var orderId))
        {
            Console.WriteLine(
                $"[Webhook] PaymentIntent {intent.Id} has no order_id metadata");
            return Response<bool>.Fail("order_id not found in metadata");
        }

        Console.WriteLine($"[Webhook] Finalizing order {orderId}");
        return await _orderService.FinalizeOrder(orderId);
    }

    private async Task<Response<bool>> HandlePaymentFailed(Stripe.Event stripeEvent)
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        if (intent == null)
            return Response<bool>.Fail("Event payload missing PaymentIntent");

        if (!intent.Metadata.TryGetValue("order_id", out var orderIdStr) ||
            !Guid.TryParse(orderIdStr, out var orderId))
        {
            return Response<bool>.Fail("order_id not found in metadata");
        }

        // Mark the order's payment as failed. We do NOT delete the order — it stays
        // as an audit trail. The user could create a new intent on the same order
        // (your create-intent endpoint already supports the idempotent path).
        var order = await _client.From<Model.Models.Order.Order>()
            .Where(o => o.Id == orderId).Single();

        if (order != null && order.PaymentStatus == "pending")
        {
            order.PaymentStatus = "failed";
            await _client.From<Model.Models.Order.Order>().Update(order);
            Console.WriteLine($"[Webhook] Marked order {orderId} as payment_failed");
        }

        return Response<bool>.SuccessResponse(true, "Failure recorded");
    }
}
