using Microsoft.AspNetCore.Diagnostics;

namespace SmartWarehouse.Api.Errors;

public sealed class WarehouseExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not WarehouseException ex)
            return false;

        httpContext.Response.StatusCode = ex.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(ex.Error, cancellationToken);
        return true;
    }
}
