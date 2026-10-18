using Audiophile.Application.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Audiophile.Infrastructure.Payments;

// Safety net 2: every few minutes, closes unpaid gateway orders whose payment window has
// passed (30 min card/wallet, 48 h kiosk) and gives their stock back. Without it, a customer
// who opens the payment page and walks away would hold that stock forever.
public sealed class PaymentExpiryWorker(IServiceScopeFactory scopes, ILogger<PaymentExpiryWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // A background service is a singleton; OrderService and the DbContext are
                // scoped, so each run gets its own scope (like one HTTP request).
                await using var scope = scopes.CreateAsyncScope();
                var orders = scope.ServiceProvider.GetRequiredService<OrderService>();
                var expired = await orders.ExpireStalePaymentsAsync(stoppingToken);
                if (expired > 0)
                    logger.LogInformation("Expired {Count} unpaid orders", expired);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Payment expiry run failed; will try again next interval");
            }
        }
    }
}
