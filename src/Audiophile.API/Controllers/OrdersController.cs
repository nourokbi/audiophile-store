using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Application.Orders;
using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

[ApiController]
[Authorize]
[Route("orders")]
public sealed class OrdersController(OrderService _ordersService, PaymentService _paymentsService, ICurrentUser currentUser) : ControllerBase
{
  // Places an order from the cart, a card order's response carries the payment page link in payment.checkoutUrl
  [HttpPost]
  public async Task<IActionResult> Place(
      PlaceOrderRequest request,
      [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
      CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > Order.IdempotencyKeyMaxLength)
      return Problem(statusCode: 400, title: "Invalid request",
          detail: $"An Idempotency-Key header of 1 to {Order.IdempotencyKeyMaxLength} characters is required.");

    var result = await _ordersService.PlaceOrderAsync(currentUser.Id, request, idempotencyKey.Trim(), ct);
    return result.Created
        ? CreatedAtAction(nameof(Get), new { id = result.Order.Id }, result.Order)
        : Ok(result.Order);
  }

  [HttpGet]
  public async Task<IActionResult> List(CancellationToken ct) =>
      Ok(await _ordersService.ListOrdersAsync(currentUser.Id, ct));

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
      Ok(await _ordersService.GetOrderAsync(currentUser.Id, id, ct));

  // The customer cancels their own order, which returns the stock, but a paid card order needs support for the refund
  [HttpPost("{id:guid}/cancel")]
  public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
      Ok(await _ordersService.CancelOrderAsync(currentUser.Id, id, ct));

  // Opens a new Fawaterak payment page for the customer's unpaid card order, since a failed attempt closes the old one
  [HttpPost("{id:guid}/payment/retry")]
  public async Task<IActionResult> RetryPayment(Guid id, CancellationToken ct) =>
      Ok(await _paymentsService.RetryPaymentAsync(currentUser.Id, id, ct));

  // The Fawaterak notifications stored for any order, a support tool for admins
  [HttpGet("{id:guid}/payment/events")]
  [Authorize(Roles = Roles.Admin)]
  public async Task<IActionResult> Events(Guid id, CancellationToken ct) =>
      Ok(await _paymentsService.GetPaymentEventsAsync(id, ct));

  // Asks Fawaterak for the real payment status when a webhook seems to be missing, for admins
  [HttpPost("{id:guid}/payment/sync")]
  [Authorize(Roles = Roles.Admin)]
  public async Task<IActionResult> Sync(Guid id, CancellationToken ct) =>
      Ok(await _paymentsService.SyncWithGatewayAsync(id, ct));
}
