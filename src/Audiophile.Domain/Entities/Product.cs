using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

// A catalogue item whose stock only changes through DecreaseStock and Restock, so it can never go negative
public sealed class Product
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int FeaturesMaxLength = 4000;
    public const int SlugMaxLength = 100;
    public const int MaxImages = 8;

    // Not readonly because EF Core fills it from the jsonb InTheBox column
    private Dictionary<string, int> _inTheBox = new(StringComparer.OrdinalIgnoreCase);

    // Not readonly because EF Core fills it from the jsonb Images column
    private List<string> _images = [];

    private Product() { } // EF Core

    public Product(
        string name,
        string description,
        decimal price,
        int stock,
        ProductCategory category,
        string? features = null,
        IEnumerable<string>? images = null,
        string? slug = null)
    {
        Name = Guard.Text(name, 2, NameMaxLength, nameof(name));
        Slug = ValidSlug(slug ?? ToSlug(Name));
        Description = Guard.Text(description, 10, DescriptionMaxLength, nameof(description));
        Price = Guard.PositiveMoney(price, nameof(price));
        Stock = Guard.AtLeast(stock, 0, nameof(stock));
        Category = Guard.Defined(category, nameof(category));
        Features = Guard.OptionalText(features, FeaturesMaxLength, nameof(features)) ?? string.Empty;
        SetImages(images);
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; } = null!;

    // The product's address in the shop, set once so saved links keep working after a rename
    public string Slug { get; private set; } = null!;

    public string Description { get; private set; } = null!;
    public string Features { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    // The photos in the order the shop shows them, the first is the cover on the cards, the cart and the orders
    public IReadOnlyList<string> Images => _images;
    public string? ImageUrl => _images.FirstOrDefault();

    public bool IsActive { get; private set; } = true;
    public ProductCategory Category { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, int> InTheBox => _inTheBox;

    // A friendly check while shopping, checkout decreases the stock and checks again
    public bool IsAvailable(int quantity = 1) => IsActive && Stock >= Guard.AtLeast(quantity, 1, nameof(quantity));

    public void UpdateDetails(string name, string description, ProductCategory category, string? features = null)
    {
        // Everything is validated before anything changes, so a rejected edit leaves the product as it was
        var newName = Guard.Text(name, 2, NameMaxLength, nameof(name));
        var newDescription = Guard.Text(description, 10, DescriptionMaxLength, nameof(description));
        var newCategory = Guard.Defined(category, nameof(category));
        var newFeatures = Guard.OptionalText(features, FeaturesMaxLength, nameof(features)) ?? string.Empty;

        Name = newName;
        Description = newDescription;
        Category = newCategory;
        Features = newFeatures;
    }

    // Turns a name like XX99 Mark II Headphones into xx99-mark-ii-headphones
    public static string ToSlug(string text)
    {
        var chars = text.Trim().ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-');
        var slug = string.Join('-', new string(chars.ToArray()).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length <= SlugMaxLength ? slug : slug[..SlugMaxLength].TrimEnd('-');
    }

    private static string ValidSlug(string slug)
    {
        var trimmed = Guard.Text(slug, 2, SlugMaxLength, nameof(slug));
        if (trimmed != ToSlug(trimmed))
            throw new ArgumentException("slug may only contain lowercase letters, digits and single hyphens.", nameof(slug));
        return trimmed;
    }

    public void ChangePrice(decimal newPrice) => Price = Guard.PositiveMoney(newPrice, nameof(newPrice));

    // Blank entries are dropped and a repeated address is kept once, so the list holds only real photos
    public void SetImages(IEnumerable<string>? images)
    {
        var validImages = (images ?? [])
            .Select(image => Guard.OptionalUrl(image, "images"))
            .OfType<string>()
            .Distinct()
            .ToList();
        if (validImages.Count > MaxImages)
            throw new ArgumentException($"A product can have up to {MaxImages} images.", nameof(images));

        _images = validImages;
    }

    public void ReplaceBoxContents(IEnumerable<KeyValuePair<string, int>> contents)
    {
        _inTheBox.Clear();
        foreach (var (item, quantity) in contents)
            _inTheBox[Guard.Text(item, 1, 100, "item")] = Guard.AtLeast(quantity, 1, "quantity");
    }

    public void Restock(int quantity) => Stock += Guard.AtLeast(quantity, 1, nameof(quantity));

    // An admin sets the stock after counting the shelf
    public void CorrectStock(int count) => Stock = Guard.AtLeast(count, 0, nameof(count));

    // Refuses instead of going below zero, which the API turns into a 409
    public void DecreaseStock(int quantity)
    {
        Guard.AtLeast(quantity, 1, nameof(quantity));

        if (Stock < quantity)
            throw new InvalidOperationException($"'{Name}' has {Stock} in stock; {quantity} were requested.");

        Stock -= quantity;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
