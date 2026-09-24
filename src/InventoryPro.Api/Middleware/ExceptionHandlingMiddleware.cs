using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;

namespace InventoryPro.Api.Middleware;

/// <summary>
/// Catches exceptions from anywhere in the pipeline and converts them into the
/// standard ApiResponse envelope. This is the .NET equivalent of @ControllerAdvice
/// with @ExceptionHandler methods in Spring Boot.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
            _logger.LogWarning(exception, "Handled application exception: {Message}", exception.Message);
            await WriteResponseAsync(context, exception.StatusCode, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message, statusCode);

        await context.Response.WriteAsJsonAsync(response);
    }
}
