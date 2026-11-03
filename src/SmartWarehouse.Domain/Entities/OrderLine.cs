namespace SmartWarehouse.Domain.Entities;

public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ShelfId { get; set; }
    public Shelf Shelf { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QtyCartons { get; set; }
    public string Status { get; set; } = "";
    public int PickSequence { get; set; }
    public int AssignedWorkerId { get; set; }
    public User AssignedWorker { get; set; } = null!;
    public DateTime? PickedAt { get; set; }
    public DateTime? HandedOffAt { get; set; }
}
