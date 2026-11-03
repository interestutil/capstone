using SmartWarehouse.Contracts;

namespace SmartWarehouse.AdminWeb.Models;

public sealed class CreateOrderViewModel
{
    public int BranchId { get; set; }
    public DateTime Deadline { get; set; } = DateTime.Now.AddHours(4);
    public List<CreateOrderLineForm> Lines { get; set; } = [new()];
    public List<BranchDto> Branches { get; set; } = [];
    public List<ProductDto> Products { get; set; } = [];
    public List<ShelfDto> Shelves { get; set; } = [];
    public List<WorkerDto> Workers { get; set; } = [];
}

public sealed class CreateOrderLineForm
{
    public int ProductId { get; set; }
    public int ShelfId { get; set; }
    public int QtyCartons { get; set; } = 1;
    public int AssignedWorkerId { get; set; }
}

public sealed class InventoryPageViewModel
{
    public List<ShelfDto> Shelves { get; set; } = [];
    public List<InventoryRowDto> Totals { get; set; } = [];
}

public sealed class InboundPageViewModel
{
    public List<InboundDto> Records { get; set; } = [];
    public List<ShelfDto> Shelves { get; set; } = [];
    public List<ProductDto> Products { get; set; } = [];
}

public sealed class OrderListViewModel
{
    public string? Status { get; set; }
    public List<OrderSummaryDto> Orders { get; set; } = [];
}

public sealed class OrderDetailPageViewModel
{
    public OrderDetailDto Order { get; set; } = null!;
    public List<WorkerDto> Workers { get; set; } = [];
}
