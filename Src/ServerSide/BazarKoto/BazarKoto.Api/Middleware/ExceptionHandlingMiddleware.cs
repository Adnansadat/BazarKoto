using System.Net;
using System.Text.Json;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request was cancelled by the client.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var errors = _environment.IsDevelopment()
                ? new[] { exception.Message }
                : ["An unexpected error occurred."];

            var response = ApiResponse<object>.Fail("Request failed.", errors);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }
}
