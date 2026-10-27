using Audiophile.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Common;

// Shared by every service that changes stock, because a checkout, a cancel and a restock can touch the same product at once
public static class StockConflictRetry
{
  // Runs the work once more on fresh data when another request changed the same stock, a second conflict becomes a 409
  public static async Task<T> RetryOnStockConflictAsync<T>(this IAppDbContext db, ILogger logger, Func<Task<T>> work)
  {
    try
    {
      return await work();
    }
    catch (DbUpdateConcurrencyException)
    {
      logger.LogInformation("Stock changed by another request, trying once more");
      db.ChangeTracker.Clear();
      return await work();
    }
  }
}
