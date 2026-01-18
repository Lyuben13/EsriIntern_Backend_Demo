using EsriIntern.Api.Dtos;
using System.Net;
using System.Text.Json;

namespace EsriIntern.Api.Middleware;

/// <summary>
/// Middleware за централизирана обработка на необработени exceptions
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponseDto
        {
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case InvalidOperationException invalidOpEx:
                response.Status = StatusCodes.Status400BadRequest;
                response.Message = invalidOpEx.Message;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            case KeyNotFoundException:
                response.Status = StatusCodes.Status404NotFound;
                response.Message = "Resource not found";
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                break;

            case UnauthorizedAccessException:
                response.Status = StatusCodes.Status401Unauthorized;
                response.Message = "Unauthorized access";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                break;

            case OperationCanceledException:
                response.Status = StatusCodes.Status499ClientClosedRequest;
                response.Message = "Request was cancelled";
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                break;

            default:
                response.Status = StatusCodes.Status500InternalServerError;
                response.Message = "An error occurred while processing your request";
                
                // В Development режим показваме stack trace
                if (_env.IsDevelopment())
                {
                    response.Details = exception.ToString();
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}
