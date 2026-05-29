using BazarKoto.Application.Interfaces;

namespace BazarKoto.Api.Services;

public class HttpUserTrackingRequestContextAccessor : IUserTrackingRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserTrackingRequestContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? RawIpAddress => GetRawIpAddress(_httpContextAccessor.HttpContext);

    public string? RawUserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private static string? GetRawIpAddress(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        var forwardedIp = forwardedFor
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedIp))
        {
            return forwardedIp;
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].ToString();

        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
