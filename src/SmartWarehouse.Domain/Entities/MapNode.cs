namespace SmartWarehouse.Domain.Entities;

public sealed class MapNode
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string Type { get; set; } = "";
}
