using SmartWarehouse.Contracts;

namespace SmartWarehouse.AdminWeb.Api;

public sealed class WarehouseApiException(int statusCode, ApiError error) : Exception(error.Message)
{
    public int StatusCode { get; } = statusCode;
    public ApiError Error { get; } = error;

    public string DisplayMessage
    {
        get
        {
            if (Error.ShelfId is int shelfId)
            {
                return $"{Error.Message} Shelf {shelfId}: requested {Error.Requested}, available {Error.Available}.";
            }

            return Error.Message;
        }
    }
}
