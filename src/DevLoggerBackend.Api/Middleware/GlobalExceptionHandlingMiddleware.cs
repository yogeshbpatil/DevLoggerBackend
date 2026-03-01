using DevLoggerBackend.Api.Models;
using DevLoggerBackend.Application.Common.Exceptions;
using FluentValidation;
using System.Text.Json;

namespace DevLoggerBackend.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse { TraceId = context.TraceIdentifier };

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Validation failed.";
                response.Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                break;
            case NotFoundException:
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = exception.Message;
                break;
            case UnauthorizedException:
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = exception.Message;
                break;
            case ConflictException:
                response.StatusCode = StatusCodes.Status409Conflict;
                response.Message = exception.Message;
                break;
            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "An unexpected error occurred.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;
        var payload = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(payload);
    }
}
