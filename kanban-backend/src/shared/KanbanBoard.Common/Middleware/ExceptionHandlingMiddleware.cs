using System.Net;
using System.Text.Json;
using KanbanBoard.Common.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KanbanBoard.Common.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            UnauthorizedAccessException => new ApiResponse
            {
                Success = false,
                Message = "Unauthorized access",
                StatusCode = (int)HttpStatusCode.Unauthorized
            },
            ArgumentException => new ApiResponse
            {
                Success = false,
                Message = exception.Message,
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            KeyNotFoundException => new ApiResponse
            {
                Success = false,
                Message = "Resource not found",
                StatusCode = (int)HttpStatusCode.NotFound
            },
            _ => new ApiResponse
            {
                Success = false,
                Message = "An internal server error occurred",
                StatusCode = (int)HttpStatusCode.InternalServerError
            }
        };

        context.Response.StatusCode = response.StatusCode;
        
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(jsonResponse);
    }
}