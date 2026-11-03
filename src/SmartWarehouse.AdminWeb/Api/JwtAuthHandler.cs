using System.Net.Http.Headers;

namespace SmartWarehouse.AdminWeb.Api;

public sealed class JwtAuthHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    public const string TokenCookie = "wms.api";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = accessor.HttpContext?.Request.Cookies[TokenCookie];
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
