using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Application.Carts;

// The customer's cart, the rules about its items live in the Cart entity
public sealed class CartService(IAppDbContext db)
{
    private const int MaxGuestCartLines = 100;

    public async Task<CartDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var cart = await FindCartAsync(userId, ct);
        return cart is null ? CartDto.Empty : CartDto.From(cart);
    }

    // Adding a product that is already in the cart tops up its line.
    public async Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, ct)
            ?? throw new NotFoundException($"Product {request.ProductId} does not exist.");

        var cart = await FindCartAsync(userId, ct);
        if (cart is null)
        {
            cart = new Cart(userId);
            db.Carts.Add(cart);
        }

        var alreadyInCart = cart.FindItem(product.Id)?.Quantity ?? 0;
        EnsureInStock(product, alreadyInCart + request.Quantity);

        cart.AddItem(product.Id, request.Quantity);
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartDto> SetItemQuantityAsync(Guid userId, Guid productId, int quantity, CancellationToken ct)
    {
        var cart = await FindCartAsync(userId, ct);
        var item = cart?.FindItem(productId)
            ?? throw new NotFoundException($"Product {productId} is not in your cart.");

        EnsureInStock(item.Product, quantity);

        cart!.SetItemQuantity(productId, quantity);
        await db.SaveChangesAsync(ct);
        return CartDto.From(cart);
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid productId, CancellationToken ct)
    {
        var cart = await FindCartAsync(userId, ct);
        if (cart?.FindItem(productId) is null)
            throw new NotFoundException($"Product {productId} is not in your cart.");

        cart.RemoveItem(productId);
        await db.SaveChangesAsync(ct);
        return CartDto.From(cart);
    }

    public async Task<CartDto> ClearAsync(Guid userId, CancellationToken ct)
    {
        var cart = await FindCartAsync(userId, ct);
        if (cart is not null)
        {
            cart.Clear();
            await db.SaveChangesAsync(ct);
        }
        return CartDto.Empty;
    }

    // Prices the cart a visitor keeps in the browser, hidden products drop out and each line is capped at the stock
    public async Task<CartDto> QuoteAsync(IReadOnlyList<GuestCartItem> items, CancellationToken ct)
    {
        var wanted = WantedQuantities(items);
        var products = await ActiveProductsAsync(wanted.Keys, ct);

        return CartDto.From(products
            .Select(p => (Product: p, Quantity: CappedQuantity(p, wanted[p.Id])))
            .Where(l => l.Quantity > 0));
    }

    // Moves the visitor's cart into the account after login, a line that would pass the stock is capped so the login never fails
    public async Task<CartDto> MergeAsync(Guid userId, IReadOnlyList<GuestCartItem> items, CancellationToken ct)
    {
        var wanted = WantedQuantities(items);
        var products = await ActiveProductsAsync(wanted.Keys, ct);

        var cart = await FindCartAsync(userId, ct);
        if (cart is null)
        {
            cart = new Cart(userId);
            db.Carts.Add(cart);
        }

        foreach (var product in products)
        {
            var alreadyInCart = cart.FindItem(product.Id)?.Quantity ?? 0;
            var toAdd = CappedQuantity(product, alreadyInCart + wanted[product.Id]) - alreadyInCart;
            if (toAdd > 0)
                cart.AddItem(product.Id, toAdd);
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    private Task<Cart?> FindCartAsync(Guid userId, CancellationToken ct) => db.Carts
        .Include(c => c.CartItems).ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    private Task<List<Product>> ActiveProductsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        db.Products.Where(p => ids.Contains(p.Id) && p.IsActive).ToListAsync(ct);

    // One quantity per product, a product listed twice is added up
    private static Dictionary<Guid, int> WantedQuantities(IReadOnlyList<GuestCartItem> items)
    {
        if (items.Count > MaxGuestCartLines)
            throw new ArgumentException($"A cart cannot have more than {MaxGuestCartLines} lines.", nameof(items));

        return items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => Math.Min(i.Quantity, CartItem.MaxQuantity)));
    }

    private static int CappedQuantity(Product product, int quantity) =>
        Math.Min(quantity, Math.Min(product.Stock, CartItem.MaxQuantity));

    // Tells the customer early that stock is short, checkout checks again when it decreases the stock
    private static void EnsureInStock(Product product, int quantity)
    {
        if (!product.IsAvailable(quantity))
            throw new ConflictException($"Only {product.Stock} of '{product.Name}' left in stock.");
    }
}
