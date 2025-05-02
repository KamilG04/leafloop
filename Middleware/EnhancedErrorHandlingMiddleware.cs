using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using LeafLoop.Models.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions in a standardized way across the application
    /// </summary>
    public class EnhancedErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public EnhancedErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<EnhancedErrorHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
                
                // Handle 404 errors for API requests
                if (context.Response.StatusCode == 404 && IsApiRequest(context.Request))
                {
                    context.Response.ContentType = "application/json";
                    var response = ApiResponse<object>.ErrorResponse("The requested resource was not found");
                    await WriteJsonResponseAsync(context, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // For API requests, return JSON error response
            if (IsApiRequest(context.Request))
            {
                await HandleApiExceptionAsync(context, exception);
                return;
            }
            
            // For MVC requests, handle differently
            await HandleMvcExceptionAsync(context, exception);
        }
        
        private async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = GetStatusCode(exception);
            
            // Determine what details to include based on environment
            string errorMessage = _environment.IsDevelopment() 
                ? exception.Message 
                : "An unexpected error occurred";
            
            // Create standardized API response
            var response = ApiResponse<object>.ErrorResponse(errorMessage);
            
            // In development, include stack trace and inner exception details
            if (_environment.IsDevelopment())
            {
                // Add stack trace and inner exception info to development responses
                response.DevDetails = new
                {
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }
            
            await WriteJsonResponseAsync(context, response);
        }
        
        private async Task HandleMvcExceptionAsync(HttpContext context, Exception exception)
        {
            // For MVC requests, redirect to Error page
            context.Response.Redirect($"/Home/Error?id={Guid.NewGuid()}");
            
            // Log the error with a correlation ID that could be shown on the error page
            _logger.LogError(exception, "MVC Error: {Message}", exception.Message);
            
            await Task.CompletedTask;
        }
        
        private async Task WriteJsonResponseAsync(HttpContext context, object response)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
        
        private bool IsApiRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
        }
        
        private int GetStatusCode(Exception exception)
        {
            // Map common exceptions to appropriate HTTP status codes
            return exception switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                ArgumentException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                System.ComponentModel.DataAnnotations.ValidationException => StatusCodes.Status400BadRequest,
                JsonException => StatusCodes.Status400BadRequest,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                TimeoutException => StatusCodes.Status504GatewayTimeout,
                // Default server error
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }

    // Extension method for middleware registration
    public static class EnhancedErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnhancedErrorHandlingMiddleware>();
        }
    }
}