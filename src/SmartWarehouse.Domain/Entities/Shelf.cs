namespace SmartWarehouse.Domain.Entities;

public sealed class Shelf
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int Rack { get; set; }
    public int Level { get; set; }
    public int ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = null!;
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int MaxCapacity { get; set; }
    public int OnHand { get; set; }
    public int Committed { get; set; }
    public int NodeId { get; set; }
    public MapNode Node { get; set; } = null!;
    public string? LedDeviceId { get; set; }
    public int LedIndex { get; set; }

    public int Available => OnHand - Committed;
    public bool IsFull => OnHand >= MaxCapacity;
    public bool IsEmpty => OnHand == 0 && Committed == 0;
}
