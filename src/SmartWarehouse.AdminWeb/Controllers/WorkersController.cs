using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.AdminWeb.Api;

namespace SmartWarehouse.AdminWeb.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class WorkersController(WarehouseApiClient api) : Controller
{
    public async Task<IActionResult> Index() => View(await api.GetWorkersAsync());
}
