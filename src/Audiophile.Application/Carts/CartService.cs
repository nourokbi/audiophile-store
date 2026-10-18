using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Application.Carts;

// The customer's cart, the rules about its items live in the Cart entity
public sealed class CartService(IAppDbContext db)
{
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

    private Task<Cart?> FindCartAsync(Guid userId, CancellationToken ct) => db.Carts
        .Include(c => c.CartItems).ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    // Tells the customer early that stock is short, checkout checks again when it decreases the stock
    private static void EnsureInStock(Product product, int quantity)
    {
        if (!product.IsAvailable(quantity))
            throw new ConflictException($"Only {product.Stock} of '{product.Name}' left in stock.");
    }
}
