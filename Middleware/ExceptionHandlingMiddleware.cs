using System;
using System.Net;
using System.Threading.Tasks;
using LeafLoop.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using LeafLoop.Models.API;

namespace LeafLoop.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Only handle API requests (skip for MVC page requests)
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                // For non-API requests, rethrow the exception to be handled by the default error handling
                throw exception;
            }
            
            // For API requests, return a proper JSON response
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = GetStatusCode(exception);
            
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An unexpected error occurred",
                Data = _environment.IsDevelopment() 
                    ? new { stackTrace = exception.StackTrace } 
                    : null
            };
            
            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }

        private int GetStatusCode(Exception exception) => exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            // Add other specific exception types as needed
            _ => StatusCodes.Status500InternalServerError
        };
    }

    // Extension method to make middleware registration cleaner
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}