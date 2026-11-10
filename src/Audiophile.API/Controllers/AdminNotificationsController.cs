using Audiophile.Application.Common;
using Audiophile.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.API.Controllers;

// The dashboard's bell: new orders, cancellations, refunds to send and low stock
[ApiController]
[Route("admin/notifications")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminNotificationsController(AdminNotifications _notificationsService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> List(CancellationToken ct, int limit = AdminNotificationList.DefaultLimit) =>
      Ok(await _notificationsService.ListAsync(limit, ct));

  [HttpPost("{id:guid}/read")]
  public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
  {
    await _notificationsService.MarkReadAsync(id, ct);
    return NoContent();
  }

  [HttpPost("read-all")]
  public async Task<IActionResult> MarkAllRead(CancellationToken ct)
  {
    await _notificationsService.MarkAllReadAsync(ct);
    return NoContent();
  }
}
