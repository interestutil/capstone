namespace SmartWarehouse.Contracts;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, int UserId, string FullName, string Role, string Email);

public record BranchDto(int Id, string Name);

public record ProductDto(
    int Id,
    string Name,
    int ProductTypeId,
    string ProductTypeName,
    string PieceBarcode,
    string CartonBarcode);

public record ShelfDto(
    int Id,
    string Code,
    int Rack,
    int Level,
    int ProductTypeId,
    string ProductTypeName,
    int? ProductId,
    string? ProductName,
    int MaxCapacity,
    int OnHand,
    int Committed,
    int Available,
    bool IsEmpty,
    bool IsFull,
    string? LedDeviceId,
    int LedIndex);

public record InventoryRowDto(
    int ProductId,
    string ProductName,
    int OnHand,
    int Committed,
    int Available,
    int ShelfCount);

public record WorkerDto(int Id, string Email, string FullName, bool IsActive);

public record CreateOrderRequest(int BranchId, DateTime Deadline, List<CreateOrderLineRequest> Lines);

public record CreateOrderLineRequest(int ShelfId, int ProductId, int QtyCartons, int AssignedWorkerId);

public record OrderSummaryDto(
    int Id,
    string BranchName,
    DateTime Deadline,
    string Status,
    DateTime CreatedAt,
    int LineCount);

public record OrderDetailDto(
    int Id,
    int BranchId,
    string BranchName,
    DateTime Deadline,
    string Status,
    DateTime CreatedAt,
    DateTime? FirstPickAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    List<OrderLineDto> Lines);

public record OrderLineDto(
    int Id,
    int ShelfId,
    string ShelfCode,
    int ProductId,
    string ProductName,
    int QtyCartons,
    string Status,
    int PickSequence,
    int AssignedWorkerId,
    string AssignedWorkerName,
    DateTime? PickedAt,
    DateTime? HandedOffAt);

public record ReassignWorkerRequest(int WorkerId);

public record ConfirmPickRequest(string? Barcode);

public record CreateInboundRequest(string Barcode, int QtyCartons, string VoucherNumber);

public record AssignShelfRequest(int ShelfId);

public record InboundDto(
    int Id,
    string VoucherNumber,
    int ProductId,
    string ProductName,
    int QtyCartons,
    int? TargetShelfId,
    string? TargetShelfCode,
    string Status,
    int CreatedByWorkerId,
    string CreatedByWorkerName,
    DateTime CreatedAt,
    DateTime? PlacedAt);

public record MapInfoDto(string ImageUrl, int Width, int Height);

public record RouteDto(int OrderId, List<RouteLegDto> Legs);

public record RouteLegDto(
    int Sequence,
    string Kind,
    int? ShelfId,
    int? LineId,
    List<PointDto> Points,
    double Distance);

public record PointDto(double X, double Y);

public record LedDto(int LedIndex, bool On, string? ShelfCode);

public record ApiError(
    string Message,
    int? ShelfId = null,
    int? Requested = null,
    int? Available = null,
    int? NodeId = null);
