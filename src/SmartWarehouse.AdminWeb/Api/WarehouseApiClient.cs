using System.Net.Http.Json;
using System.Text.Json;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.AdminWeb.Api;

public sealed class WarehouseApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", request, ct);

    public Task<List<BranchDto>> GetBranchesAsync(CancellationToken ct = default) =>
        SendAsync<List<BranchDto>>(HttpMethod.Get, "api/branches", null, ct);

    public Task<List<ProductDto>> GetProductsAsync(CancellationToken ct = default) =>
        SendAsync<List<ProductDto>>(HttpMethod.Get, "api/products", null, ct);

    public Task<List<ShelfDto>> GetShelvesAsync(CancellationToken ct = default) =>
        SendAsync<List<ShelfDto>>(HttpMethod.Get, "api/shelves", null, ct);

    public Task<List<InventoryRowDto>> GetInventoryAsync(CancellationToken ct = default) =>
        SendAsync<List<InventoryRowDto>>(HttpMethod.Get, "api/inventory", null, ct);

    public Task<List<WorkerDto>> GetWorkersAsync(CancellationToken ct = default) =>
        SendAsync<List<WorkerDto>>(HttpMethod.Get, "api/workers", null, ct);

    public Task<List<OrderSummaryDto>> GetOrdersAsync(string? status = null, CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(status) ? "api/orders" : $"api/orders?status={Uri.EscapeDataString(status)}";
        return SendAsync<List<OrderSummaryDto>>(HttpMethod.Get, path, null, ct);
    }

    public Task<OrderDetailDto> GetOrderAsync(int id, CancellationToken ct = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Get, $"api/orders/{id}", null, ct);

    public Task<OrderDetailDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Post, "api/orders", request, ct);

    public Task<OrderDetailDto> CancelOrderAsync(int id, CancellationToken ct = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Post, $"api/orders/{id}/cancel", null, ct);

    public Task<OrderDetailDto> ReassignLineAsync(int orderId, int lineId, int workerId, CancellationToken ct = default) =>
        SendAsync<OrderDetailDto>(HttpMethod.Put, $"api/orders/{orderId}/lines/{lineId}/worker", new ReassignWorkerRequest(workerId), ct);

    public Task<List<InboundDto>> GetInboundAsync(CancellationToken ct = default) =>
        SendAsync<List<InboundDto>>(HttpMethod.Get, "api/inbound", null, ct);

    public Task<InboundDto> AssignInboundShelfAsync(int id, int shelfId, CancellationToken ct = default) =>
        SendAsync<InboundDto>(HttpMethod.Put, $"api/inbound/{id}/shelf", new AssignShelfRequest(shelfId), ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);

        using var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return default!;

            return await response.Content.ReadFromJsonAsync<T>(Json, ct)
                   ?? throw new WarehouseApiException(500, new ApiError("The API returned an empty body."));
        }

        ApiError error;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiError>(Json, ct)
                    ?? new ApiError($"API error {(int)response.StatusCode}");
        }
        catch (JsonException)
        {
            error = new ApiError($"API error {(int)response.StatusCode}");
        }

        throw new WarehouseApiException((int)response.StatusCode, error);
    }
}
