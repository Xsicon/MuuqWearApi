using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuuqWear.API.Shared;
using MuuqWear.Application.Controllers;
using MuuqWear.Application.Interfaces;
using MuuqWear.Model.DTO.Payment;
using Stripe;

namespace MuuqWear.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;

    public PaymentController(IPaymentService paymentService, IConfiguration config)
    {
        _paymentService = paymentService;
        _config = config;
    }

    /// <summary>
    /// Create (or fetch existing) Stripe PaymentIntent for an order.
    /// Returns the client_secret the frontend uses to confirm payment.
    /// </summary>
    [HttpPost("create-intent/{orderId}")]
    public async Task<ActionResult<Response<CreatePaymentIntentResultDTO>>> CreateIntent(
        Guid orderId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(Response<CreatePaymentIntentResultDTO>.Fail("Not authenticated"));

        var result = await _paymentService.CreatePaymentIntent(orderId, userId);
        return HandleResponse(result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]   // Stripe doesn't authenticate; signature is the proof
    public async Task<IActionResult> Webhook()
    {
        // Read the raw body — must NOT be deserialized first or signature breaks
        string json;
        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync();
        }

        var signatureHeader = Request.Headers["Stripe-Signature"];
        var webhookSecret = _config["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            Console.WriteLine("[Webhook] Stripe:WebhookSecret not configured");
            return StatusCode(500);
        }

        Stripe.Event stripeEvent;
        try
        {
            // This is the signature check. If the request body was tampered with,
            // or the signature header is missing/wrong, this throws.
            stripeEvent = Stripe.EventUtility.ConstructEvent(
                json, signatureHeader, webhookSecret);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"[Webhook] Signature verification failed: {ex.Message}");
            return BadRequest();   // 400 — Stripe will retry, but won't accept invalid forever
        }


        // Hand off to the service. The HTTP response below is just acknowledgment;
        // anything that happens here is acted on AFTER we tell Stripe "got it".
        var result = await _paymentService.HandleWebhookEvent(stripeEvent);

        // Always return 200 to Stripe, even on internal errors.
        // Otherwise Stripe will retry the webhook for 3 days. If something's
        // genuinely broken on our side, the user-visible UI handles it; the
        // webhook just needs to confirm we received it.
        if (!result.Success)
        {
            Console.WriteLine($"[Webhook] Handler returned failure: {result.Message}");
        }
        return Ok();
    }
}
