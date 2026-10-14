// PAYMENT GATEWAY (off): the expiry job only exists for gateway payments, so it is
// commented out while the cart and checkout are built. See the note at the top of
// Application/Orders/OrderService.cs.

// using Audiophile.Application.Orders;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
//
// namespace Audiophile.Infrastructure.Payments;
//
// // Every few minutes, closes unpaid gateway orders whose payment window has passed.
// public sealed class PaymentExpiryWorker(IServiceScopeFactory scopes, ILogger<PaymentExpiryWorker> logger)
//     : BackgroundService
// {
//     private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         using var timer = new PeriodicTimer(Interval);
//         while (await timer.WaitForNextTickAsync(stoppingToken))
//         {
//             try
//             {
//                 await using var scope = scopes.CreateAsyncScope();
//                 var orders = scope.ServiceProvider.GetRequiredService<OrderService>();
//                 var expired = await orders.ExpireStalePaymentsAsync(stoppingToken);
//                 if (expired > 0)
//                     logger.LogInformation("Expired {Count} unpaid orders", expired);
//             }
//             catch (Exception ex) when (ex is not OperationCanceledException)
//             {
//                 logger.LogError(ex, "Payment expiry run failed; will try again next interval");
//             }
//         }
//     }
// }
