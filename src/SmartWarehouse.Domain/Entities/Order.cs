namespace SmartWarehouse.Domain.Entities;

public sealed class Order
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? FirstPickAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public ICollection<OrderLine> Lines { get; set; } = [];
}
