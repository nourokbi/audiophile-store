using Audiophile.Application.Payments;
using Audiophile.Infrastructure.Payments.Fawaterak;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Audiophile.API.Controllers;

// The two URLs Fawaterak sends traffic to. Both are public (no [Authorize]):
// Fawaterak's server has no JWT, and the customer's browser arrives from Fawaterak's site.
// Trust comes from the webhook's signature instead.
[ApiController]
[Route("payments/fawaterak")]
public sealed class FawaterakController(
    IFawaterakGateway fawaterak,
    PaymentResultApplier applier,
    IOptions<FawaterakOptions> options,
    ILogger<FawaterakController> logger) : ControllerBase
{
  // FLOW 6: Fawaterak's server-to-server notification, sent to the webhookUrl we gave it in
  // FLOW 4. "_json" in the path makes Fawaterak send JSON; the plain path is kept for the
  // dashboard's default setting. The only thing allowed to mark an order paid.
  [HttpPost("webhook_json")]
  [HttpPost("webhook")]
  public async Task<IActionResult> Webhook(CancellationToken ct)
  {
    // The raw text, not a model: the signature is checked against exactly what was sent.
    using var reader = new StreamReader(Request.Body);
    var rawBody = await reader.ReadToEndAsync(ct);

    // FLOW 7: verify hashKey, then ask Fawaterak whether the invoice is really paid.
    var callback = await fawaterak.ParseWebhookAsync(rawBody, ct);

    if (!callback.IsAuthentic)
      return Unauthorized();                 // forged or wrong vendor key: change nothing

    if (callback.Transaction is null)
    {
      // A declined attempt or a refund notice: logged, nothing to change.
      logger.LogInformation("Fawaterak {Type} webhook acknowledged without action", callback.EventType);
      return Ok();
    }

    // FLOW 8: payment Succeeded → order Confirmed (or Duplicate, AmountMismatch, …).
    var outcome = await applier.ApplyAsync(IFawaterakGateway.Provider, "webhook", callback.Transaction, rawBody, ct);

    // Any 2xx tells Fawaterak "received"; a non-2xx makes it retry about 40 seconds later.
    return Ok(new { outcome = outcome.ToString() });
  }

  // FLOW 9: where the customer's browser lands after paying (success, fail and pending all
  // come here). Forwards to the client, which shows our own record of the order (GET /orders/{id}).
  // Fawaterak's redirect is not signed, so "result" only picks the wording while the page
  // waits for the webhook, and never changes the order.
  [HttpGet("return")]
  public IActionResult Return([FromQuery] Guid orderId, [FromQuery] string? result)
  {
    var safeResult = result is "success" or "fail" or "pending" ? result : "pending";
    var client = options.Value.ClientReturnUrl.TrimEnd('/');
    return Redirect($"{client}?orderId={orderId}&result={safeResult}");
  }
}
