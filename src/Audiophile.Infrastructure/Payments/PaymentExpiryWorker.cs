using Audiophile.Application.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Audiophile.Infrastructure.Payments;

// Every five minutes cancels card orders nobody paid in time, so an abandoned payment page does not hold stock forever
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
                // Each run gets its own scope because the services and the DbContext are scoped
                await using var scope = scopes.CreateAsyncScope();
                var payments = scope.ServiceProvider.GetRequiredService<PaymentService>();
                var expiredCount = await payments.ExpireUnpaidOrdersAsync(stoppingToken);
                if (expiredCount > 0)
                    logger.LogInformation("Expired {Count} unpaid orders", expiredCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Payment expiry run failed, will try again next interval");
            }
        }
    }
}
