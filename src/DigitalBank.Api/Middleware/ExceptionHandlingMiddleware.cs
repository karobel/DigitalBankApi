using System.Net;
using System.Text.Json;
using DigitalBank.Domain.Exceptions;

namespace DigitalBank.Api.Middleware;

/// <summary>
/// Catches exceptions thrown anywhere in the pipeline and converts them into
/// consistent JSON error responses, so controllers stay free of try/catch noise.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            AccountNotFoundException or CustomerNotFoundException =>
                (HttpStatusCode.NotFound, exception.Message),

            InsufficientFundsException or InvalidTransferException or AccountNotActiveException =>
                (HttpStatusCode.BadRequest, exception.Message),

            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized, exception.Message),

            InvalidOperationException =>
                (HttpStatusCode.Conflict, exception.Message),

            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}) processing {Method} {Path}: {Message}",
                (int)statusCode, context.Request.Method, context.Request.Path, message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            status = (int)statusCode,
            error = statusCode.ToString(),
            message,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
