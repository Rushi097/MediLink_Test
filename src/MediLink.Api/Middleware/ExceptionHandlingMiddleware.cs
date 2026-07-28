using System.Net;

namespace MediLink.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error for {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted) throw;
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "An unexpected server error occurred.", traceId = context.TraceIdentifier });
        }
    }
}
