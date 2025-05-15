using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using Microsoft.AspNetCore.Http; // For StatusCodes
using Microsoft.AspNetCore.Mvc; // For ControllerBase, IActionResult, etc.
using Microsoft.AspNetCore.Mvc.ModelBinding; // For ModelStateDictionary
using Microsoft.Extensions.DependencyInjection; // For GetService<T>
using Microsoft.Extensions.Logging; // For ILogger
using System; // For Exception, object?
using System.Linq; // For LINQ methods on ModelStateDictionary

namespace LeafLoop.Api
{
    /// <summary>
    /// Provides extension methods for <see cref="ControllerBase"/> to create standardized API responses.
    /// </summary>
    public static class ApiControllerExtensions
    {
        // --- Success Responses ---

        /// <summary>
        /// Creates a successful (200 OK) API response with data.
        /// </summary>
        /// <typeparam name="T">The type of the data in the response.</typeparam>
        /// <param name="controller">The controller instance.</param>
        /// <param name="data">The data to include in the response.</param>
        /// <param name="message">An optional success message.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 200 OK response with an <see cref="ApiResponse{T}"/>.</returns>
        public static IActionResult ApiOk<T>(
            this ControllerBase controller, T data, string? message = null)
        {
            return controller.Ok(ApiResponse<T>.SuccessResponse(data, message));
        }

        /// <summary>
        /// Creates a successful (200 OK) API response without specific data.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="message">An optional success message.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 200 OK response with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiOk(
            this ControllerBase controller, string? message = null)
        {
            return controller.Ok(ApiResponse.SuccessResponse(message));
        }

        /// <summary>
        /// Creates a successful (200 OK) API response with paginated data.
        /// </summary>
        /// <typeparam name="T">The type of the paginated data in the response.</typeparam>
        /// <param name="controller">The controller instance.</param>
        /// <param name="data">The paginated data to include in the response.</param>
        /// <param name="totalItems">The total number of items available.</param>
        /// <param name="totalPages">The total number of pages available.</param>
        /// <param name="currentPage">The current page number.</param>
        /// <param name="message">An optional success message.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 200 OK response with an <see cref="ApiResponse{T}"/> including pagination details.</returns>
        public static IActionResult ApiOkWithPagination<T>(
            this ControllerBase controller, T data, int totalItems, int totalPages, int currentPage, string? message = null)
        {
            return controller.Ok(ApiResponse<T>.SuccessResponse(data, totalItems, totalPages, currentPage, message));
        }

        /// <summary>
        /// Creates a Created (201) API response using a route name for location header.
        /// </summary>
        /// <typeparam name="T">The type of the created resource data.</typeparam>
        /// <param name="controller">The controller instance.</param>
        /// <param name="data">The data of the created resource.</param>
        /// <param name="routeName">The name of the route to generate the location URL.</param>
        /// <param name="routeValues">The route values to generate the location URL.</param>
        /// <param name="message">An optional message indicating success.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 201 Created response with an <see cref="ApiResponse{T}"/>.</returns>
        public static IActionResult ApiCreated<T>(
            this ControllerBase controller, T data, string routeName, object routeValues, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message ?? "Resource created successfully.");
            return controller.CreatedAtRoute(routeName, routeValues, response);
        }

        /// <summary>
        /// Creates a Created (201) API response using action and controller names for location header.
        /// </summary>
        /// <typeparam name="T">The type of the created resource data.</typeparam>
        /// <param name="controller">The controller instance.</param>
        /// <param name="data">The data of the created resource.</param>
        /// <param name="actionName">The name of the action method.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The route values to generate the location URL.</param>
        /// <param name="message">An optional message indicating success.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 201 Created response with an <see cref="ApiResponse{T}"/>.</returns>
        public static IActionResult ApiCreatedAtAction<T>(
            this ControllerBase controller, T data, string actionName, string controllerName, object routeValues, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message ?? "Resource created successfully.");
            return controller.CreatedAtAction(actionName, controllerName, routeValues, response);
        }

        /// <summary>
        /// Creates a standard HTTP 204 No Content response.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 204 No Content response.</returns>
        public static IActionResult ApiNoContent(this ControllerBase controller)
        {
            return controller.NoContent();
        }

        // --- Error Responses (using non-generic ApiResponse) ---

        /// <summary>
        /// Creates a generic error API response with a specified status code.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="statusCode">The HTTP status code for the error.</param>
        /// <param name="message">The primary error message.</param>
        /// <param name="errors">Optional details about the errors (e.g., validation errors).</param>
        /// <returns>An <see cref="IActionResult"/> representing the specified error status code with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiError(
            this ControllerBase controller, int statusCode, string message, object? errors = null)
        {
            return controller.StatusCode(statusCode, ApiResponse.ErrorResponse(message, errors));
        }

        /// <summary>
        /// Creates a BadRequest (400) API response.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="message">The error message for the bad request.</param>
        /// <param name="errors">Optional details about the errors.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 400 Bad Request response with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiBadRequest(
            this ControllerBase controller, string message, object? errors = null)
        {
            return controller.BadRequest(ApiResponse.ErrorResponse(message, errors));
        }

        /// <summary>
        /// Creates a BadRequest (400) API response from a <see cref="ModelStateDictionary"/>.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="modelState">The model state containing validation errors.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 400 Bad Request response with validation errors from model state.</returns>
        public static IActionResult ApiBadRequest(
            this ControllerBase controller, ModelStateDictionary modelState)
        {
            if (modelState.IsValid) // Should ideally not happen if this method is called due to invalid model state.
            {
                return controller.BadRequest(ApiResponse.ErrorResponse("Validation failed.", null));
            }

            var errorDictionary = modelState
                .Where(kvp => kvp.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return controller.BadRequest(ApiResponse.ErrorResponse("Validation failed. Please check the errors.", errorDictionary));
        }

        /// <summary>
        /// Creates a NotFound (404) API response.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="message">An optional message indicating the resource was not found.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 404 Not Found response with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiNotFound(
            this ControllerBase controller, string message = "Resource not found.")
        {
            return controller.NotFound(ApiResponse.ErrorResponse(message));
        }

        /// <summary>
        /// Creates a Forbidden (403) API response.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="message">An optional message indicating access is forbidden.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 403 Forbidden response with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiForbidden(
            this ControllerBase controller, string message = "Access denied. You do not have permission to perform this action.")
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse(message));
        }

        /// <summary>
        /// Creates an Unauthorized (401) API response.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="message">An optional message indicating authentication is required.</param>
        /// <returns>An <see cref="IActionResult"/> representing a 401 Unauthorized response with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiUnauthorized(
            this ControllerBase controller, string message = "Unauthorized. Authentication is required and has failed or has not yet been provided.")
        {
            return controller.StatusCode(StatusCodes.Status401Unauthorized, ApiResponse.ErrorResponse(message));
        }

        /// <summary>
        /// Creates an Internal Server Error (500) API response.
        /// Logs the error and optionally includes exception details in DEBUG mode.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="message">A user-friendly message for the internal server error.</param>
        /// <param name="exception">The exception that occurred (optional).</param>
        /// <returns>An <see cref="IActionResult"/> representing a 500 Internal Server Error response with a non-generic <see cref="ApiResponse"/>.</returns>
        public static IActionResult ApiInternalError(
            this ControllerBase controller,
            string message = "An unexpected internal server error occurred. Please try again later.",
            Exception? exception = null)
        {
            object? devDetails = null;
            try
            {
                // Attempt to get a logger specific to the controller type if possible,
                // otherwise a generic logger for ControllerBase.
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<ControllerBase>>(); 
                // Fallback or more specific logger: var logger = controller.HttpContext.RequestServices.GetService<ILogger(controller.GetType())>();


                if (logger != null)
                {
                    if (exception != null)
                    {
                        logger.LogError(exception, "Internal Server Error occurred (via ApiInternalError extension): {UserMessage}", message);
                    }
                    else
                    {
                        logger.LogError("Internal Server Error occurred without exception details (via ApiInternalError extension): {UserMessage}", message);
                    }
                }
            }
            catch
            {
                // Ignore logging errors to ensure the error response is still sent.
            }

            // Include detailed exception information only in DEBUG builds for security.
            // The preprocessor directive #if DEBUG ... #endif ensures this code is only compiled in Debug configuration.
            #if DEBUG
            if (exception != null)
            {
                devDetails = new
                {
                    Type = exception.GetType().FullName, // Using FullName for more detail
                    Detail = exception.Message,
                    StackTrace = exception.StackTrace // Be cautious about exposing stack traces even in devDetails.
                };
            }
            #endif

            return controller.StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse(message, devDetails: devDetails));
        }
    }
}
