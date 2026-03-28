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

            // Wrap empty error responses (e.g. 401 from [Authorize], 404 from routing, 429 from rate limiter)
            if (!context.Response.HasStarted && context.Response.StatusCode >= 400
                && context.Response.ContentLength is null or 0
                && context.Response.ContentType is null)
            {
                var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
                var message = context.Response.StatusCode switch
                {
                    401 => "Authentication is required.",
                    403 => "You do not have permission to perform this action.",
                    404 => "The requested resource was not found.",
                    405 => "HTTP method not allowed.",
                    429 => "Too many requests. Please slow down.",
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

            var (statusCode, message) = ex switch
            {
                KeyNotFoundException => (404, "The requested resource was not found."),
                UnauthorizedAccessException => (403, "You do not have permission to perform this action."),
                ArgumentException argEx => (400, argEx.Message),
                _ => (500, "An unexpected error occurred. Please try again later.")
            };

            if (statusCode >= 500)
                logger.LogError(ex, "Unhandled server error. CorrelationId: {CorrelationId}", correlationId);
            else
                logger.LogWarning("Client error {StatusCode}: {Message}. CorrelationId: {CorrelationId}",
                    statusCode, ex.Message, correlationId);

            if (context.Response.HasStarted)
            {
                logger.LogWarning("Cannot write error response; response already started. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            try
            {
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                var response = ApiResponse<object>.Error(message, statusCode, correlationId);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
            }
            catch (Exception writeEx)
            {
                logger.LogError(writeEx, "Failed to write error response. CorrelationId: {CorrelationId}", correlationId);
            }
        }
    }
}
