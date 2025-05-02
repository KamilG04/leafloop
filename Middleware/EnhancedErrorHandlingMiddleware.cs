using System;
using System.Collections.Generic; // Potrzebne dla KeyNotFoundException
using System.ComponentModel.DataAnnotations; // Potrzebne dla ValidationException
using System.Net; // Potrzebne dla HttpStatusCode (chociaż używamy StatusCodes)
using System.Text.Json;
using System.Text.Json.Serialization; // <<<=== DODANO USING
using System.Threading.Tasks;
using LeafLoop.Models.API; // Dla ApiResponse (niegeneryczne)
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting; // Dla IHostEnvironment i IsDevelopment()
using Microsoft.Extensions.Logging;

namespace LeafLoop.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions in a standardized way across the application.
    /// Returns JSON ApiResponse for API requests and redirects for MVC requests.
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

                // Opcjonalnie: Obsługa 404 dla API po przejściu przez inne middleware
                if (context.Response.StatusCode == StatusCodes.Status404NotFound
                    && !context.Response.HasStarted
                    && IsApiRequest(context.Request))
                {
                    _logger.LogWarning("API Resource not found (404): {Path}", context.Request.Path);
                    context.Response.ContentType = "application/json";
                    var response = ApiResponse.ErrorResponse("The requested API resource was not found.");
                    await WriteJsonResponseAsync(context, response, StatusCodes.Status404NotFound);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception caught by middleware: {Message}", ex.Message);
                if (!context.Response.HasStarted)
                {
                    await HandleExceptionAsync(context, ex);
                }
                else
                {
                     _logger.LogWarning("Response has already started, cannot handle exception with custom response.");
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (IsApiRequest(context.Request))
            {
                await HandleApiExceptionAsync(context, exception);
            }
            else
            {
                await HandleMvcExceptionAsync(context, exception);
            }
        }

        private async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            int statusCode = GetStatusCode(exception);
            context.Response.StatusCode = statusCode;

            string userMessage = "An unexpected error occurred. Please try again later.";
            object? errors = null;
            object? devDetails = null;

            switch (exception)
            {
                case KeyNotFoundException knfe:
                    userMessage = knfe.Message;
                    break;
                case UnauthorizedAccessException uae:
                    userMessage = uae.Message;
                    break;
                case ArgumentException ae:
                    userMessage = "Invalid request data provided.";
                    errors = new { ArgumentError = ae.Message };
                    break;
                 case ValidationException ve:
                    userMessage = "Validation failed.";
                    errors = new { ValidationErrors = ve.ValidationResult?.ErrorMessage };
                     break;
                case InvalidOperationException ioe:
                    userMessage = ioe.Message;
                    break;
            }

            if (_environment.IsDevelopment())
            {
                userMessage = exception.Message;
                devDetails = new
                {
                    Type = exception.GetType().Name,
                    InnerException = exception.InnerException?.Message
                };
            }

            var response = ApiResponse.ErrorResponse(userMessage, errors, devDetails);
            await WriteJsonResponseAsync(context, response, statusCode);
        }

        private Task HandleMvcExceptionAsync(HttpContext context, Exception exception) // Zmienna nazywa się 'exception'
        {
            var errorId = Guid.NewGuid();
            // === POPRAWIONA LINIA 140 ===
            // Użyj zmiennej 'exception' zamiast 'ex'
            _logger.LogError(exception, "Unhandled MVC exception occurred. Error ID: {ErrorId}", errorId);
            // === KONIEC POPRAWKI ===

             context.Items["ExceptionHandled"] = true;
             context.Response.Redirect($"/Home/Error?errorId={errorId}");

            return Task.CompletedTask;
        }

        private async Task WriteJsonResponseAsync(HttpContext context, object response, int statusCode)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // === POPRAWIONA LINIA 161 ===
                // Teraz JsonIgnoreCondition jest rozpoznawane dzięki using System.Text.Json.Serialization;
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                // === KONIEC POPRAWKI ===
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }

        private bool IsApiRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                   (request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
                   !request.Headers["Accept"].ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase));
        }

        private int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                ArgumentException => StatusCodes.Status400BadRequest,
                ValidationException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                JsonException => StatusCodes.Status400BadRequest,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                TimeoutException => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }

    // Metoda rozszerzająca do rejestracji middleware
    public static class EnhancedErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnhancedErrorHandlingMiddleware>();
        }
    }
}
