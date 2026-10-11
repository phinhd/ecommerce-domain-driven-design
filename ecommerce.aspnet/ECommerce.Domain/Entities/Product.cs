namespace ECommerce.Domain.Entities;

public class Product : BaseClass
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public static Product Create(Guid ownerId, string name, string? description, decimal price, int quantity, string? imageUrl, DateTime nowUtc)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = ownerId,
            CreatedAt = nowUtc
        };
        product.SetDetails(name, description, price, quantity, imageUrl);
        return product;
    }

    public bool IsDeleted() => DeletedAt is not null;

    public bool IsOwnedBy(Guid userId) => CreatedByUserId == userId;

    public void UpdateDetails(string name, string? description, decimal price, int quantity, string? imageUrl, DateTime nowUtc)
    {
        SetDetails(name, description, price, quantity, imageUrl);
        UpdatedAt = nowUtc;
    }

    public void SoftDelete(DateTime nowUtc)
    {
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    private void SetDetails(string name, string? description, decimal price, int quantity, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        Name = name.Trim();
        Description = description;
        Price = price;
        Quantity = quantity;
        ImageUrl = imageUrl;
    }
}
