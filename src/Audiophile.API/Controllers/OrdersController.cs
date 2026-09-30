using Audiophile.Application.Common;
using Audiophile.Application.Orders;
using Audiophile.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    // Places an order. For card, wallet and kiosk the response carries
    // payment.checkoutUrl: send the customer there. Sending the same
    // Idempotency-Key again returns the original order (200) instead of a new one (201).
    [HttpPost]
    public async Task<IActionResult> Place(
        PlaceOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > Order.IdempotencyKeyMaxLength)
            return Problem(statusCode: 400, title: "Invalid request",
                detail: $"An Idempotency-Key header of 1 to {Order.IdempotencyKeyMaxLength} characters is required.");

        var result = await orders.PlaceOrderAsync(DemoUser.Id, request, idempotencyKey.Trim(), ct);
        return result.Created
            ? CreatedAtAction(nameof(Get), new { id = result.Order.Id }, result.Order)
            : Ok(result.Order);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await orders.ListOrdersAsync(DemoUser.Id, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok(await orders.GetOrderAsync(DemoUser.Id, id, ct));

    // The two below are support tools. Refunds are made in the Fawaterak dashboard:
    // Fawaterak has no public refund API.

    // The Fawaterak notifications stored for this order (support / audit view).
    [HttpGet("{id:guid}/payment/events")]
    public async Task<IActionResult> Events(Guid id, CancellationToken ct) =>
        Ok(await orders.GetPaymentEventsAsync(id, ct));

    // Asks Fawaterak for the real status. Use when a webhook seems to be missing.
    [HttpPost("{id:guid}/payment/sync")]
    public async Task<IActionResult> Sync(Guid id, CancellationToken ct) =>
        Ok(await orders.SyncWithGatewayAsync(id, ct));
}
