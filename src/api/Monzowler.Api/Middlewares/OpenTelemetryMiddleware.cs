using OpenTelemetry.Trace;

namespace Monzowler.Api.Middlewares;

public class OpenTelemetryMiddleware(RequestDelegate next, Tracer tracer, ILogger<OpenTelemetryMiddleware> logger)
{
    /// <summary>
    /// Generic Middleware so we can get the error responses appropiately from our controller
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        using var span = tracer.StartActiveSpan("HTTP Request", SpanKind.Server);
        try
        {
            span.SetAttribute("http.method", context.Request.Method);
            span.SetAttribute("http.url", context.Request.Path);
            span.SetAttribute("http.query", context.Request.QueryString.ToString());

            await next(context);

            span.SetAttribute("http.status_code", context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            span.SetStatus(Status.Error.WithDescription(ex.Message));

            throw;
        }
    }
}