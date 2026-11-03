using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.Api.Services;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class OrdersController(OrderService orders, RoutingService routing) : ControllerBase
{
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("orders")]
    public async Task<ActionResult<OrderDetailDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await orders.CreateAsync(CurrentUser.Id(User), request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderSummaryDto>>> List([FromQuery] string? status, CancellationToken ct) =>
        Ok(await orders.ListAsync(status, ct));

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> Get(int id, CancellationToken ct) =>
        Ok(await orders.GetAsync(id, CurrentUser.Id(User), CurrentUser.Role(User), ct));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("orders/{id:int}/cancel")]
    public async Task<ActionResult<OrderDetailDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await orders.CancelAsync(id, ct));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("orders/{id:int}/lines/{lineId:int}/worker")]
    public async Task<ActionResult<OrderDetailDto>> Reassign(int id, int lineId, ReassignWorkerRequest request, CancellationToken ct) =>
        Ok(await orders.ReassignAsync(id, lineId, request.WorkerId, ct));

    [Authorize(Roles = Roles.Worker)]
    [HttpGet("my/orders")]
    public async Task<ActionResult<List<OrderDetailDto>>> Mine(CancellationToken ct) =>
        Ok(await orders.GetMineAsync(CurrentUser.Id(User), ct));

    [Authorize(Roles = Roles.Worker)]
    [HttpPost("orders/{id:int}/lines/{lineId:int}/pick")]
    public async Task<ActionResult<OrderDetailDto>> Pick(int id, int lineId, ConfirmPickRequest? request, CancellationToken ct) =>
        Ok(await orders.ConfirmPickAsync(id, lineId, CurrentUser.Id(User), request?.Barcode, ct));

    [Authorize(Roles = Roles.Worker)]
    [HttpPost("orders/{id:int}/handoff")]
    public async Task<ActionResult<OrderDetailDto>> Handoff(int id, CancellationToken ct) =>
        Ok(await orders.ConfirmHandoffAsync(id, CurrentUser.Id(User), ct));

    [Authorize(Roles = Roles.Worker)]
    [HttpGet("orders/{id:int}/route")]
    public async Task<ActionResult<RouteDto>> Route(int id, [FromQuery] int anchorShelfId, CancellationToken ct) =>
        Ok(await routing.GetRouteAsync(id, CurrentUser.Id(User), anchorShelfId, ct));
}
