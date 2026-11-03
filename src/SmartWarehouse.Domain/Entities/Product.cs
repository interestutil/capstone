namespace SmartWarehouse.Domain.Entities;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = null!;
    public string PieceBarcode { get; set; } = "";
    public string CartonBarcode { get; set; } = "";
}
