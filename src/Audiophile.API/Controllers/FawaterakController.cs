using Audiophile.Application.Payments;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

[ApiController]
[Route("payments/fawaterak")]
public sealed class FawaterakController(
    IPaymentGatewayResolver gateways,
    PaymentResultApplier applier,
    ILogger<FawaterakController> logger) : ControllerBase
{
    private const string Provider = "fawaterak";

    // Fawaterak's server-to-server notification. "_json" in the path makes
    // Fawaterak send JSON; the plain path is kept for the dashboard's default setting.
    // The only thing allowed to mark an order paid.
    [HttpPost("webhook_json")]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        var gateway = gateways.Get(Provider);
        var callback = await gateway.ParseWebhookAsync(
            rawBody, Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()), ct);

        if (!callback.IsAuthentic)
            return Unauthorized();

        if (callback.Transaction is null)
        {
            logger.LogInformation("Fawaterak {Type} webhook acknowledged without action", callback.EventType);
            return Ok();
        }

        var outcome = await applier.ApplyAsync(Provider, "webhook", callback.Transaction, rawBody, ct);
        return Ok(new { outcome = outcome.ToString() });
    }

    // Where the customer's browser lands after paying (success, fail and pending all
    // come here). Forwards to My orders, which shows our own record of the order:
    // Fawaterak's redirect is not signed, so "result" only picks the wording while
    // the page waits for the webhook, and never changes the order.
    [HttpGet("return")]
    public IActionResult Return([FromQuery] Guid orderId, [FromQuery] string? result)
    {
        var safeResult = result is "success" or "fail" or "pending" ? result : "pending";
        return Redirect($"/orders.html?id={orderId}&result={safeResult}");
    }
}
