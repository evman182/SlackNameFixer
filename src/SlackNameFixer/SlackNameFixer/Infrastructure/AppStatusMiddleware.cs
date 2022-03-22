using System.Net;

namespace SlackNameFixer.Infrastructure;

public class AppStatusMiddleware
{
    private readonly RequestDelegate _next;

    public AppStatusMiddleware(RequestDelegate next) =>
        _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.ToString() == "/app_status")
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync("OK");
            return;
        }

        await _next(context);
    }
}