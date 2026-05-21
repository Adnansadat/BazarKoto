using System.Security.Cryptography;
using System.Text;

namespace BazarKoto.Api.Middleware;

public class RequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTrackingMiddleware> _logger;

    public RequestTrackingMiddleware(RequestDelegate next, ILogger<RequestTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipHash = HashValue(context.Connection.RemoteIpAddress?.ToString());
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = context.TraceIdentifier,
            ["IpHash"] = ipHash
        });

        _logger.LogInformation("HTTP {Method} {Path} started.", context.Request.Method, context.Request.Path);
        await _next(context);
        _logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode}.", context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }

    private static string HashValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
