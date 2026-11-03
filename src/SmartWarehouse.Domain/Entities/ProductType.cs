namespace SmartWarehouse.Domain.Entities;

public sealed class ProductType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Shelf> Shelves { get; set; } = [];
}
