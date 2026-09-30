using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

// A catalogue item. Stock only ever moves through DecreaseStock
// and Restock, which is what makes "stock cannot go negative" a
// property of the type rather than a rule every caller has to remember.
public sealed class Product
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int FeaturesMaxLength = 4000;

    // Case-insensitive: "Headphone Unit" and "headphone unit" are one box entry, not two.
    private readonly Dictionary<string, int> _inTheBox = new(StringComparer.OrdinalIgnoreCase);

    private Product() { } // EF Core

    public Product(
        string name,
        string description,
        decimal price,
        int stock,
        ProductCategory category,
        string? features = null,
        string? imageUrl = null)
    {
        Name = Guard.Text(name, 2, NameMaxLength, nameof(name));
        Description = Guard.Text(description, 10, DescriptionMaxLength, nameof(description));
        Price = Guard.PositiveMoney(price, nameof(price));
        Stock = Guard.AtLeast(stock, 0, nameof(stock));
        Category = Guard.Defined(category, nameof(category));
        Features = Guard.OptionalText(features, FeaturesMaxLength, nameof(features)) ?? string.Empty;
        ImageUrl = Guard.OptionalUrl(imageUrl, nameof(imageUrl));
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Features { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ProductCategory Category { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Persisted as jsonb; mutated through the box methods, never assigned wholesale.
    public IReadOnlyDictionary<string, int> InTheBox => _inTheBox;

    // Buyable: listed, and enough on hand. Never the last word — checkout re-checks under a transaction (M4-05).
    public bool IsAvailable(int quantity = 1) => IsActive && Stock >= Guard.AtLeast(quantity, 1, nameof(quantity));

    public void UpdateDetails(string name, string description, ProductCategory category, string? features = null)
    {
        // Validate everything first: a rejected edit must not leave the product
        // with a new name and its old description.
        var newName = Guard.Text(name, 2, NameMaxLength, nameof(name));
        var newDescription = Guard.Text(description, 10, DescriptionMaxLength, nameof(description));
        var newCategory = Guard.Defined(category, nameof(category));
        var newFeatures = Guard.OptionalText(features, FeaturesMaxLength, nameof(features)) ?? string.Empty;

        Name = newName;
        Description = newDescription;
        Category = newCategory;
        Features = newFeatures;
    }

    public void ChangePrice(decimal newPrice) => Price = Guard.PositiveMoney(newPrice, nameof(newPrice));

    public void SetImage(string? imageUrl) => ImageUrl = Guard.OptionalUrl(imageUrl, nameof(imageUrl));

    public void PutInBox(string item, int quantity) =>
        _inTheBox[Guard.Text(item, 1, 100, nameof(item))] = Guard.AtLeast(quantity, 1, nameof(quantity));

    public void RemoveFromBox(string item)
    {
        if (!_inTheBox.Remove(Guard.Text(item, 1, 100, nameof(item))))
            throw new InvalidOperationException($"'{item}' is not in this product's box.");
    }

    public void ReplaceBoxContents(IEnumerable<KeyValuePair<string, int>> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);

        // Materialise before clearing, in case the caller handed us a lazy query
        // over the very dictionary we are about to empty.
        var replacement = contents.ToList();
        _inTheBox.Clear();
        foreach (var (item, quantity) in replacement)
            PutInBox(item, quantity);
    }

    public void Restock(int quantity) => Stock += Guard.AtLeast(quantity, 1, nameof(quantity));

    // Called inside the checkout transaction. Refusing here rather than clamping
    // to zero is what lets the loser of a race become a 409 (M4-06).
    public void DecreaseStock(int quantity)
    {
        Guard.AtLeast(quantity, 1, nameof(quantity));

        if (Stock < quantity)
            throw new InvalidOperationException($"'{Name}' has {Stock} in stock; {quantity} were requested.");

        Stock -= quantity;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
