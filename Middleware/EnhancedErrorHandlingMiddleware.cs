using System;
using System.Collections.Generic; // For KeyNotFoundException if it were used more broadly, not strictly needed here
using System.ComponentModel.DataAnnotations; // For ValidationException
using System.Net;
using System.Security; // For HttpStatusCode, though StatusCodes class is preferred
using System.Text.Json;
using System.Text.Json.Serialization; // For JsonIgnoreCondition
using System.Threading.Tasks;
using LeafLoop.Models.API; // For ApiResponse (non-generic)
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting; // For IWebHostEnvironment and IsDevelopment()
using Microsoft.Extensions.Logging;

namespace LeafLoop.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions in a standardized way across the application.
    /// Returns JSON ApiResponse for API requests and redirects to an error page for MVC requests.
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

                // Handle 404 Not Found for API requests if no other middleware handled it.
                // This ensures API clients get a JSON 404, not potentially an HTML 404 page.
                if (context.Response.StatusCode == StatusCodes.Status404NotFound
                    && !context.Response.HasStarted // Ensure response hasn't already started sending.
                    && IsApiRequest(context.Request))
                {
                    _logger.LogWarning("API Resource not found (404) after all other middleware: {Path}", context.Request.Path);
                    context.Response.ContentType = "application/json";
                    var response = ApiResponse.ErrorResponse("The requested API resource was not found.");
                    // No need to set context.Response.StatusCode here as it's already 404.
                    await WriteJsonResponseAsync(context, response, StatusCodes.Status404NotFound);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception caught by EnhancedErrorHandlingMiddleware: {Message}", ex.Message);
                if (!context.Response.HasStarted)
                {
                    await HandleExceptionAsync(context, ex);
                }
                else
                {
                    // If the response has already started, we can't send a new one.
                    // Log this situation as it might indicate an issue elsewhere (e.g., streaming and then erroring).
                    _logger.LogWarning("Response has already started; cannot modify response for unhandled exception.");
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
            int statusCode = GetStatusCodeForException(exception);
            context.Response.StatusCode = statusCode;

            string userMessage = "An unexpected error occurred. Please try again later.";
            object? errors = null; // For structured validation errors or similar.
            object? devDetails = null; // For development environment debugging.

            // Customize messages based on exception type.
            switch (exception)
            {
                case KeyNotFoundException knfe: // Typically indicates a resource not found by its key.
                    userMessage = knfe.Message; // Often, the exception message is suitable for the user.
                    break;
                case UnauthorizedAccessException uae:
                    userMessage = uae.Message; // Or a generic "Access Denied."
                    break;
                case ArgumentException ae when exception is not ArgumentNullException: // General argument issues.
                    userMessage = "Invalid request data provided.";
                    errors = new { ArgumentError = ae.Message }; // Provide specific argument error.
                    break;
                case ArgumentNullException ane: // Specific null argument issues.
                     userMessage = "A required parameter was null.";
                     errors = new { ArgumentNullError = ane.ParamName ?? "Unknown parameter" };
                     break;
                case ValidationException ve: // From System.ComponentModel.DataAnnotations
                    userMessage = "Validation failed. Please check your input.";
                    // errors = ve.Value; // The invalid value, if available and simple.
                    // Or, more structured:
                    errors = new { ValidationErrors = ve.ValidationResult?.ErrorMessage ?? "Invalid data" };
                    break;
                case InvalidOperationException ioe:
                    // These can be broad; sometimes the message is okay, other times too technical.
                    userMessage = _environment.IsDevelopment() ? ioe.Message : "An invalid operation was attempted.";
                    break;
                case JsonException je:
                    userMessage = "Invalid JSON format in request body.";
                    errors = new { JsonError = je.Message };
                    break;
                // TODO: Add more specific exception types as needed by the application.
                // case CustomDomainException cde:
                //     userMessage = cde.UserFriendlyMessage;
                //     errors = cde.ErrorDetails;
                //     break;
            }

            // In development, provide more detailed error information.
            if (_environment.IsDevelopment())
            {
                // Overwrite userMessage with the actual exception message for more direct feedback in dev.
                userMessage = exception.Message;
                devDetails = new
                {
                    Type = exception.GetType().FullName, // Use FullName for more clarity.
                    // StackTrace = exception.StackTrace, // Consider if StackTrace is too verbose even for devDetails.
                    InnerException = exception.InnerException != null ? new
                    {
                        Type = exception.InnerException.GetType().FullName,
                        Message = exception.InnerException.Message,
                        // StackTrace = exception.InnerException.StackTrace
                    } : null
                };
            }

            var apiResponse = ApiResponse.ErrorResponse(userMessage, errors, devDetails);
            await WriteJsonResponseAsync(context, apiResponse, statusCode);
        }

        private Task HandleMvcExceptionAsync(HttpContext context, Exception exception)
        {
            var errorId = Guid.NewGuid(); // Unique ID for correlating logs.
            _logger.LogError(exception, "Unhandled MVC exception occurred. Error ID: {ErrorId}", errorId);

            // Store the exception or error ID for the error page to potentially display.
            context.Items["ExceptionHandled"] = exception; // The error page can access this.
            context.Items["ErrorId"] = errorId;

            // Redirect to a generic error page for MVC requests.
            // Ensure this path exists and is configured to handle errors.
            context.Response.Redirect($"/Home/Error?errorId={errorId}"); // Pass errorId for tracking.

            return Task.CompletedTask;
        }

        private async Task WriteJsonResponseAsync(HttpContext context, object response, int statusCode)
        {
            // Ensure status code is set before writing (though HandleApiExceptionAsync does this).
            if (context.Response.StatusCode != statusCode) {
                 context.Response.StatusCode = statusCode;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // From System.Text.Json.Serialization
                WriteIndented = _environment.IsDevelopment() // Pretty print in development.
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }

        /// <summary>
        /// Determines if the request is likely an API request.
        /// Checks for "/api" path prefix or "application/json" in Accept header
        /// while not primarily accepting "text/html".
        /// </summary>
        private bool IsApiRequest(HttpRequest request)
        {
            // Check path first, as it's a strong indicator.
            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check Accept header for content negotiation.
            var acceptHeader = request.Headers["Accept"].ToString();
            if (acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
                !acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase)) // Avoid matching browser navigation that might also accept JSON.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Maps an exception type to an appropriate HTTP status code.
        /// </summary>
        private int GetStatusCodeForException(Exception exception)
        {
            // Prioritize more specific exceptions first.
            return exception switch
            {
                // 4xx Client Errors
                KeyNotFoundException => StatusCodes.Status404NotFound,
                FileNotFoundException => StatusCodes.Status404NotFound,
                ArgumentNullException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest, // Includes ArgumentOutOfRangeException
                ValidationException => StatusCodes.Status400BadRequest,
                JsonException => StatusCodes.Status400BadRequest, // Malformed JSON
                UnauthorizedAccessException => StatusCodes.Status403Forbidden, // Or 401 if it's about missing authentication
                SecurityException => StatusCodes.Status403Forbidden, // From System.Security

                // Custom domain exceptions could map to specific codes.
                // MyCustomValidationException => StatusCodes.Status422UnprocessableEntity,

                // 5xx Server Errors
                NotImplementedException => StatusCodes.Status501NotImplemented,
                TimeoutException => StatusCodes.Status504GatewayTimeout, // Or Status408RequestTimeout depending on context
                InvalidOperationException => StatusCodes.Status500InternalServerError, // Often indicates a server-side logic error.
                                                                                        // Can be 400 if it's due to bad client state not caught earlier.

                // Default to 500 for unmapped exceptions.
                _ => StatusCodes.Status500InternalServerError
            };
            // TODO: Consider if any InvalidOperationException should be a 400 Bad Request if it's due to client input leading to an invalid state.
        }
    }

    /// <summary>
    /// Extension method to register the <see cref="EnhancedErrorHandlingMiddleware"/> in the request pipeline.
    /// </summary>
    public static class EnhancedErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnhancedErrorHandlingMiddleware>();
        }
    }
}