namespace SmartWarehouse.Domain.Entities;

public sealed class InboundOrder
{
    public int Id { get; set; }
    public string VoucherNumber { get; set; } = "";
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QtyCartons { get; set; }
    public int? TargetShelfId { get; set; }
    public Shelf? TargetShelf { get; set; }
    public string Status { get; set; } = "";
    public int CreatedByWorkerId { get; set; }
    public User CreatedByWorker { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? PlacedAt { get; set; }
    public int? PlacedByWorkerId { get; set; }
}
