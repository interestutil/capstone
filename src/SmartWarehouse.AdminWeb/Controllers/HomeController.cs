using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.AdminWeb.Models;

namespace SmartWarehouse.AdminWeb.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Inventory");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
