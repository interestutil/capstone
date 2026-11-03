using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.Api.Services;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MapDeviceController(RoutingService routing) : ControllerBase
{
    [Authorize(Roles = Roles.Worker)]
    [HttpGet("map")]
    public async Task<ActionResult<MapInfoDto>> Map(CancellationToken ct)
    {
        var url = $"{Request.Scheme}://{Request.Host}/map.svg";
        return Ok(await routing.GetMapAsync(url, ct));
    }

    [AllowAnonymous]
    [HttpGet("devices/{deviceId}/leds")]
    public async Task<ActionResult<List<LedDto>>> Leds(string deviceId, CancellationToken ct) =>
        Ok(await routing.GetLedsAsync(deviceId, ct));
}
