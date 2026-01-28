namespace AutoQueryApiDemo.Models;

/// <summary>
/// Product entity with a custom SKU property that will be used as cursor key.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string SKU { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }

    public Product()
    {
    }

    public Product(int id, string sku, string name, decimal price)
    {
        Id = id;
        SKU = sku;
        Name = name;
        Price = price;
    }
}
