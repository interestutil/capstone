using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartWarehouse.MockApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var store = context.HttpContext.RequestServices.GetRequiredService<WarehouseStore>();
        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;
        var session = store.GetSession(token);
        if (session is null)
        {
            context.Result = new JsonResult(new { message = "Sign in required." }) { StatusCode = 401 };
            return;
        }

        if (roles.Length > 0 && !roles.Contains(session.Role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new JsonResult(new { message = "You do not have access to this action." }) { StatusCode = 403 };
            return;
        }

        var identity = new ClaimsIdentity("mock");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, session.Role));
        context.HttpContext.User = new ClaimsPrincipal(identity);
        context.HttpContext.Items["Session"] = session;
    }
}

public static class HttpContextExtensions
{
    public static WarehouseStore.Session Session(this HttpContext http) =>
        (WarehouseStore.Session)http.Items["Session"]!;
}
