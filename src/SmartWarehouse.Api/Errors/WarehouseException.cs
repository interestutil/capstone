using SmartWarehouse.Contracts;

namespace SmartWarehouse.Api.Errors;

public sealed class WarehouseException(int statusCode, ApiError error) : Exception(error.Message)
{
    public int StatusCode { get; } = statusCode;
    public ApiError Error { get; } = error;

    public static WarehouseException BadRequest(string message) => new(400, new ApiError(message));
    public static WarehouseException Unauthorized(string message) => new(401, new ApiError(message));
    public static WarehouseException Forbidden(string message) => new(403, new ApiError(message));
    public static WarehouseException NotFound(string message) => new(404, new ApiError(message));
    public static WarehouseException Conflict(string message, int? shelfId = null, int? requested = null, int? available = null) =>
        new(409, new ApiError(message, shelfId, requested, available));
    public static WarehouseException Unreachable(string message, int? nodeId = null) =>
        new(422, new ApiError(message, NodeId: nodeId));
}
