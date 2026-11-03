using System.Security.Claims;

namespace SmartWarehouse.Api.Controllers;

internal static class CurrentUser
{
    public static int Id(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string Role(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? "";
}
