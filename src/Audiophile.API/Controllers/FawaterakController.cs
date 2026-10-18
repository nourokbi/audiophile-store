using Audiophile.Application.Payments;
using Audiophile.Infrastructure.Payments.Fawaterak;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Audiophile.API.Controllers;

// The two addresses Fawaterak sends traffic to, both public because trust comes from the webhook signature
[ApiController]
[Route("payments/fawaterak")]
public sealed class FawaterakController(
    IFawaterakGateway fawaterak,
    PaymentResultApplier applier,
    IOptions<FawaterakOptions> options) : ControllerBase
{
  // Fawaterak's server tells us an invoice changed, and any 2xx answer stops it from sending again
  [HttpPost("webhook_json")]
  public async Task<IActionResult> Webhook(CancellationToken ct)
  {
    // Read as raw text because the signature is checked against exactly what was sent
    using var reader = new StreamReader(Request.Body);
    var body = await reader.ReadToEndAsync(ct);

    var paidInvoice = await fawaterak.ReadPaidInvoiceAsync(body, ct);
    if (paidInvoice is null)
      return Ok();

    var outcome = await applier.ApplyAsync("webhook", paidInvoice, body, ct);
    return Ok(new { outcome = outcome.ToString() });
  }

  // The customer's browser lands here after paying and is sent on to the shop, the result only picks the wording
  [HttpGet("return")]
  public IActionResult Return([FromQuery] Guid orderId, [FromQuery] string? result)
  {
    var safeResult = result is "success" or "fail" or "pending" ? result : "pending";
    var shopUrl = options.Value.ClientReturnUrl.TrimEnd('/');
    return Redirect($"{shopUrl}?orderId={orderId}&result={safeResult}");
  }
}
