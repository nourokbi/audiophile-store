using Audiophile.Application.Common;
using Audiophile.Application.Orders;
using Audiophile.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

[ApiController]
[Route("admin/orders")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminOrdersController(OrderService _ordersService) : ControllerBase
{
  // Every filter is optional, from and to are days like 2026-10-25
  [HttpGet]
  public async Task<IActionResult> List(
      OrderStatus? status, DateOnly? from, DateOnly? to, string? q, CancellationToken ct,
      int page = 1, int pageSize = AdminOrderQuery.DefaultPageSize) =>
      Ok(await _ordersService.ListAllOrdersAsync(new AdminOrderQuery(status, from, to, q, page, pageSize), ct));

  // The overview and income numbers for the last days, days is 1 to 365
  [HttpGet("stats")]
  public async Task<IActionResult> Stats(CancellationToken ct, int days = 30) =>
      Ok(await _ordersService.GetOrderStatsAsync(days, ct));

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
      Ok(await _ordersService.GetAnyOrderAsync(id, ct));

  // Only the order's next statuses are accepted, any other is a 409
  [HttpPut("{id:guid}/status")]
  public async Task<IActionResult> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct) =>
      Ok(await _ordersService.ChangeOrderStatusAsync(id, request.Status, ct));
}
