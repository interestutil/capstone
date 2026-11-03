using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.Api.Services;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inbound")]
public sealed class InboundController(InboundService inbound) : ControllerBase
{
    [Authorize(Roles = Roles.Worker)]
    [HttpPost]
    public async Task<ActionResult<InboundDto>> Create(CreateInboundRequest request, CancellationToken ct)
    {
        var created = await inbound.CreateAsync(CurrentUser.Id(User), request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    public async Task<ActionResult<List<InboundDto>>> List(CancellationToken ct) =>
        Ok(await inbound.ListAsync(ct));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}/shelf")]
    public async Task<ActionResult<InboundDto>> AssignShelf(int id, AssignShelfRequest request, CancellationToken ct) =>
        Ok(await inbound.AssignShelfAsync(id, request.ShelfId, ct));

    [Authorize(Roles = Roles.Worker)]
    [HttpPost("{id:int}/place")]
    public async Task<ActionResult<InboundDto>> Place(int id, CancellationToken ct) =>
        Ok(await inbound.PlaceAsync(id, CurrentUser.Id(User), ct));
}
