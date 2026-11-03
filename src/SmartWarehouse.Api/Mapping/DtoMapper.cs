using SmartWarehouse.Contracts;
using SmartWarehouse.Domain.Entities;

namespace SmartWarehouse.Api.Mapping;

public static class DtoMapper
{
    public static ProductDto ToDto(this Product p) =>
        new(p.Id, p.Name, p.ProductTypeId, p.ProductType.Name, p.PieceBarcode, p.CartonBarcode);

    public static ShelfDto ToDto(this Shelf s) =>
        new(
            s.Id, s.Code, s.Rack, s.Level, s.ProductTypeId, s.ProductType.Name,
            s.ProductId, s.Product?.Name, s.MaxCapacity, s.OnHand, s.Committed, s.Available,
            s.IsEmpty, s.IsFull, s.LedDeviceId, s.LedIndex);

    public static OrderLineDto ToDto(this OrderLine line) =>
        new(
            line.Id, line.ShelfId, line.Shelf.Code, line.ProductId, line.Product.Name, line.QtyCartons,
            line.Status, line.PickSequence, line.AssignedWorkerId, line.AssignedWorker.FullName,
            line.PickedAt, line.HandedOffAt);

    public static OrderDetailDto ToDetail(this Order order, bool includeAllLines, int? workerId = null)
    {
        var lines = includeAllLines
            ? order.Lines
            : order.Lines.Where(l => l.AssignedWorkerId == workerId);

        return new OrderDetailDto(
            order.Id, order.BranchId, order.Branch.Name, order.Deadline, order.Status,
            order.CreatedAt, order.FirstPickAt, order.CompletedAt, order.CancelledAt,
            lines.OrderBy(l => l.PickSequence).Select(ToDto).ToList());
    }

    public static OrderSummaryDto ToSummary(this Order order) =>
        new(order.Id, order.Branch.Name, order.Deadline, order.Status, order.CreatedAt, order.Lines.Count);

    public static InboundDto ToDto(this InboundOrder inbound) =>
        new(
            inbound.Id, inbound.VoucherNumber, inbound.ProductId, inbound.Product.Name, inbound.QtyCartons,
            inbound.TargetShelfId, inbound.TargetShelf?.Code, inbound.Status,
            inbound.CreatedByWorkerId, inbound.CreatedByWorker.FullName,
            inbound.CreatedAt, inbound.PlacedAt);

    public static WorkerDto ToWorkerDto(this User user) =>
        new(user.Id, user.Email, user.FullName, user.IsActive);

    public static BranchDto ToDto(this Branch branch) => new(branch.Id, branch.Name);
}
