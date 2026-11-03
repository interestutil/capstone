namespace SmartWarehouse.Domain.Entities;

public sealed class MapEdge
{
    public int Id { get; set; }
    public int FromNodeId { get; set; }
    public MapNode FromNode { get; set; } = null!;
    public int ToNodeId { get; set; }
    public MapNode ToNode { get; set; } = null!;
}
