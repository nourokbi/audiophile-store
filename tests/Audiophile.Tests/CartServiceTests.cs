using Audiophile.Application.Carts;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Tests;

// The cart against a real database, each check reads the cart back through a new DbContext to prove what was saved
public sealed class CartServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private Guid _userId;
    private Product _headphones = null!;   // 1,000 EGP, 5 in stock
    private Product _speaker = null!;      // 4,500 EGP, 10 in stock

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Adding_the_same_product_twice_tops_up_one_line()
    {
        await SeedAsync();

        await Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 1), default);
        await Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 2), default);

        var cart = await Service().GetAsync(_userId, default);
        var line = Assert.Single(cart.Items);
        Assert.Equal(3, line.Quantity);
    }

    [Fact]
    public async Task Totals_use_catalogue_prices_and_the_shipping_rule()
    {
        await SeedAsync();

        var cart = await Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 2), default);
        Assert.Equal(2_000m, cart.SubTotal);
        Assert.Equal(50m, cart.ShippingFee);          // below 5,000: flat fee
        Assert.Equal(2_050m, cart.Total);

        cart = await Service().AddItemAsync(_userId, new AddCartItemRequest(_speaker.Id, 1), default);
        Assert.Equal(6_500m, cart.SubTotal);
        Assert.Equal(0m, cart.ShippingFee);           // 5,000 or more: free
    }

    [Fact]
    public async Task Adding_more_than_is_in_stock_is_refused()
    {
        await SeedAsync();
        await Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 4), default);

        // Four already in the cart and two more make six, but only five exist
        await Assert.ThrowsAsync<ConflictException>(() =>
            Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 2), default));

        var cart = await Service().GetAsync(_userId, default);
        Assert.Equal(4, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public async Task Adding_an_unknown_product_is_not_found()
    {
        await SeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service().AddItemAsync(_userId, new AddCartItemRequest(Guid.NewGuid(), 1), default));
    }

    [Fact]
    public async Task Quantity_can_be_set_and_a_line_removed()
    {
        await SeedAsync();
        await Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 1), default);
        await Service().AddItemAsync(_userId, new AddCartItemRequest(_speaker.Id, 1), default);

        await Service().SetItemQuantityAsync(_userId, _headphones.Id, 3, default);
        await Service().RemoveItemAsync(_userId, _speaker.Id, default);

        var cart = await Service().GetAsync(_userId, default);
        var line = Assert.Single(cart.Items);
        Assert.Equal(_headphones.Id, line.ProductId);
        Assert.Equal(3, line.Quantity);
    }

    [Fact]
    public async Task Removing_a_product_that_is_not_in_the_cart_is_not_found()
    {
        await SeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service().RemoveItemAsync(_userId, _speaker.Id, default));
    }

    [Fact]
    public async Task Clear_empties_the_cart()
    {
        await SeedAsync();
        await Service().AddItemAsync(_userId, new AddCartItemRequest(_headphones.Id, 2), default);

        await Service().ClearAsync(_userId, default);

        var cart = await Service().GetAsync(_userId, default);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Total);
    }

    // A new service with a new DbContext each time, like a new HTTP request
    private CartService Service() => new(NewContext());

    private AppDbContext NewContext() =>
        _database.NewContext();

    private async Task SeedAsync()
    {
        await using var db = NewContext();

        var user = User.Create("Mona Hassan", $"{Guid.NewGuid():N}@example.com");   // comes with an empty cart
        _headphones = new Product("XX59 Headphones", "Enjoy your audio almost anywhere.", 1_000m, 5, ProductCategory.Headphones,
            slug: "test-headphones");   // the seeded XX59 already owns "xx59-headphones"
        _speaker = new Product("ZX9 Speaker", "Upgrade your sound system with the ZX9.", 4_500m, 10, ProductCategory.Speakers,
            slug: "test-speaker");

        db.AddRange(user, _headphones, _speaker);
        await db.SaveChangesAsync();
        _userId = user.Id;
    }
}
