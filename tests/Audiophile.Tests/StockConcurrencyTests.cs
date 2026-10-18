using Microsoft.EntityFrameworkCore;

namespace Audiophile.Tests;

// Proves the xmin token directly, two requests read the same stock and the second save is refused
public sealed class StockConcurrencyTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task The_second_of_two_writes_to_the_same_stock_is_refused()
    {
        var productId = await SeedProductAsync(stock: 1);

        await using var first = _database.NewContext();
        await using var second = _database.NewContext();
        var firstCopy = await first.Products.SingleAsync(p => p.Id == productId);
        var secondCopy = await second.Products.SingleAsync(p => p.Id == productId);

        firstCopy.DecreaseStock(1);
        secondCopy.DecreaseStock(1);
        await first.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var check = _database.NewContext();
        Assert.Equal(0, (await check.Products.SingleAsync(p => p.Id == productId)).Stock);
    }

    private async Task<Guid> SeedProductAsync(int stock)
    {
        await using var db = _database.NewContext();
        var product = OrderTests.Headphones(price: 1000m, stock);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }
}
