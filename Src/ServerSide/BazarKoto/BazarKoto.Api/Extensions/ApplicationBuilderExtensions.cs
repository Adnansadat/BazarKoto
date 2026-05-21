using BazarKoto.Api.Middleware;

namespace BazarKoto.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestTrackingMiddleware>();
        return app;
    }
}
