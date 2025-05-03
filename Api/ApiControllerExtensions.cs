using LeafLoop.Models.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace LeafLoop.Api
{
    public static class ApiControllerExtensions
    {
        // --- Success Responses ---

        /// <summary>Creates a successful (200 OK) API response with data.</summary>
        /// <returns>IActionResult representing 200 OK with ApiResponse<T>.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiOk<T>( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, T data, string? message = null)
        {
            // controller.Ok() zwraca OkObjectResult, który implementuje IActionResult
            return controller.Ok(ApiResponse<T>.SuccessResponse(data, message));
        }

        /// <summary>Creates a successful (200 OK) API response without specific data.</summary>
        /// <returns>IActionResult representing 200 OK with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiOk( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, string? message = null)
        {
            return controller.Ok(ApiResponse.SuccessResponse(message));
        }

        /// <summary>Creates a successful (200 OK) API response with paginated data.</summary>
        /// <returns>IActionResult representing 200 OK with ApiResponse<T>.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiOkWithPagination<T>( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, T data, int totalItems, int totalPages, int currentPage, string? message = null)
        {
            return controller.Ok(ApiResponse<T>.SuccessResponse(data, totalItems, totalPages, currentPage, message));
        }

        /// <summary>Creates a Created (201) API response using route name.</summary>
        /// <returns>IActionResult representing 201 Created with ApiResponse<T>.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiCreated<T>( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, T data, string routeName, object routeValues, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message ?? "Resource created successfully");
            // controller.CreatedAtRoute() zwraca CreatedAtRouteResult, który implementuje IActionResult
            return controller.CreatedAtRoute(routeName, routeValues, response);
        }

        /// <summary>Creates a Created (201) API response using action/controller names.</summary>
        /// <returns>IActionResult representing 201 Created with ApiResponse<T>.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiCreatedAtAction<T>( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, T data, string actionName, string controllerName, object routeValues, string? message = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message ?? "Resource created successfully");
             // controller.CreatedAtAction() zwraca CreatedAtActionResult, który implementuje IActionResult
            return controller.CreatedAtAction(actionName, controllerName, routeValues, response);
        }

        /// <summary>Creates a standard HTTP 204 No Content response (no body).</summary>
        /// <returns>IActionResult representing 204 No Content.</returns>
        public static IActionResult ApiNoContent(this ControllerBase controller)
        {
            // controller.NoContent() zwraca NoContentResult, który implementuje IActionResult (bez zmian tutaj)
            return controller.NoContent();
        }

        // --- Error Responses (using non-generic ApiResponse) ---

        /// <summary>Creates a generic error API response with status code.</summary>
        /// <returns>IActionResult representing the specified error status code with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiError( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, int statusCode, string message, object? errors = null)
        {
             // controller.StatusCode() zwraca ObjectResult (lub StatusCodeResult), który implementuje IActionResult
            return controller.StatusCode(statusCode, ApiResponse.ErrorResponse(message, errors));
        }

        /// <summary>Creates a BadRequest (400) API response.</summary>
        /// <returns>IActionResult representing 400 Bad Request with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiBadRequest( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, string message, object? errors = null)
        {
            // controller.BadRequest() zwraca BadRequestObjectResult, który implementuje IActionResult
            return controller.BadRequest(ApiResponse.ErrorResponse(message, errors));
        }

        /// <summary>Creates a BadRequest (400) API response from ModelStateDictionary.</summary>
        /// <returns>IActionResult representing 400 Bad Request with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiBadRequest( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, ModelStateDictionary modelState)
        {
            if (modelState.IsValid)
                return controller.BadRequest(ApiResponse.ErrorResponse("Validation failed.", null));

            var errorDictionary = modelState
                .Where(kvp => kvp.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return controller.BadRequest(ApiResponse.ErrorResponse("Validation failed. Please check the errors.", errorDictionary));
        }

        /// <summary>Creates a NotFound (404) API response.</summary>
        /// <returns>IActionResult representing 404 Not Found with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiNotFound( // ZMIANA: Zwraca IActionResult
            this ControllerBase controller, string message = "Resource not found")
        {
             // controller.NotFound() zwraca NotFoundObjectResult, który implementuje IActionResult
            return controller.NotFound(ApiResponse.ErrorResponse(message));
        }

        /// <summary>Creates a Forbidden (403) API response.</summary>
        /// <returns>IActionResult representing 403 Forbidden with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiForbidden( // ZMIANA: Zwraca IActionResult
           this ControllerBase controller, string message = "Access denied. You do not have permission.")
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse.ErrorResponse(message));
        }

        /// <summary>Creates an Unauthorized (401) API response.</summary>
        /// <returns>IActionResult representing 401 Unauthorized with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiUnauthorized( // ZMIANA: Zwraca IActionResult
           this ControllerBase controller, string message = "Unauthorized. Authentication is required.")
        {
           return controller.StatusCode(StatusCodes.Status401Unauthorized, ApiResponse.ErrorResponse(message));
        }

        /// <summary>Creates an Internal Server Error (500) API response.</summary>
        /// <returns>IActionResult representing 500 Internal Server Error with non-generic ApiResponse.</returns> // Zmieniono opis typu zwracanego
        public static IActionResult ApiInternalError( // ZMIANA: Zwraca IActionResult
           this ControllerBase controller,
           string message = "An unexpected internal server error occurred.",
           Exception? exception = null)
        {
            // Logowanie Błędu
            try
            {
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<ControllerBase>>();
                if (logger != null && exception != null)
                    logger.LogError(exception, "Internal Server Error occurred: {ErrorMessage}", message);
                else if (logger != null)
                     logger.LogError("Internal Server Error occurred without exception details: {ErrorMessage}", message);
            } catch { /* Ignoruj błędy logowania */ }

            // Sprawdzenie środowiska dla DevDetails
            object? devDetails = null;
            #if DEBUG
            if (exception != null)
                devDetails = new { Type = exception.GetType().Name, Detail = exception.Message, StackTrace = exception.StackTrace };
            #endif

            return controller.StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ErrorResponse(message, devDetails: devDetails));
        }
    }
}