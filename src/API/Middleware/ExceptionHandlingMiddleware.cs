using System.Text.Json;
using SphereBlog.API.Models;

namespace SphereBlog.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            // Catch empty error responses (e.g. 404 from routing, 405 method not allowed)
            if (!context.Response.HasStarted && context.Response.StatusCode >= 400
                && context.Response.ContentLength is null or 0
                && context.Response.ContentType is null)
            {
                var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
                var message = context.Response.StatusCode switch
                {
                    404 => "The requested resource was not found.",
                    405 => "HTTP method not allowed.",
                    _ => "An error occurred."
                };

                context.Response.ContentType = "application/json";
                var response = ApiResponse<object>.Error(message, context.Response.StatusCode, correlationId);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
            }
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";

            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            var (statusCode, message) = ex switch
            {
                KeyNotFoundException => (404, "The requested resource was not found."),
                UnauthorizedAccessException => (403, "You do not have permission to perform this action."),
                ArgumentException argEx => (400, argEx.Message),
                _ => (500, "An unexpected error occurred. Please try again later.")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Error(message, statusCode, correlationId);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
