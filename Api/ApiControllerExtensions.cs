using LeafLoop.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace LeafLoop.Api
{
    /// <summary>
    /// Extension methods for API controllers to provide consistent response formats
    /// </summary>
    public static class ApiControllerExtensions
    {
        /// <summary>
        /// Creates a successful API response
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="data">Data to return</param>
        /// <param name="message">Optional success message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiOk<T>(
            this ControllerBase controller, 
            T data, 
            string message = null)
        {
            return controller.Ok(ApiResponse<T>.SuccessResponse(data, message));
        }
        
        /// <summary>
        /// Creates a successful API response with pagination data
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="data">Data to return</param>
        /// <param name="totalItems">Total number of items</param>
        /// <param name="totalPages">Total number of pages</param>
        /// <param name="currentPage">Current page number</param>
        /// <param name="message">Optional success message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiOkWithPagination<T>(
            this ControllerBase controller,
            T data,
            int totalItems,
            int totalPages,
            int currentPage,
            string message = null)
        {
            return controller.Ok(ApiResponse<T>.SuccessResponse(
                data, totalItems, totalPages, currentPage, message));
        }
        
        /// <summary>
        /// Creates an error API response
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="statusCode">HTTP status code</param>
        /// <param name="message">Error message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiError<T>(
            this ControllerBase controller,
            int statusCode,
            string message)
        {
            return controller.StatusCode(statusCode, ApiResponse<T>.ErrorResponse(message));
        }
        
        /// <summary>
        /// Creates a BadRequest (400) API response
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="message">Error message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiBadRequest<T>(
            this ControllerBase controller,
            string message)
        {
            return controller.BadRequest(ApiResponse<T>.ErrorResponse(message));
        }
        
        /// <summary>
        /// Creates a NotFound (404) API response
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="message">Error message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiNotFound<T>(
            this ControllerBase controller,
            string message)
        {
            return controller.NotFound(ApiResponse<T>.ErrorResponse(message));
        }
        
        /// <summary>
        /// Creates a Created (201) API response
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="data">Data to return</param>
        /// <param name="routeName">Name of the route to use for generating URI</param>
        /// <param name="routeValues">Route values for generating URI</param>
        /// <param name="message">Optional success message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiCreated<T>(
            this ControllerBase controller,
            T data,
            string routeName,
            object routeValues,
            string message = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message);
            return controller.CreatedAtRoute(routeName, routeValues, response);
        }
        
        /// <summary>
        /// Creates a Created (201) API response with an action
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="controller">Controller instance</param>
        /// <param name="data">Data to return</param>
        /// <param name="actionName">Name of the action to use for generating URI</param>
        /// <param name="controllerName">Name of the controller to use for generating URI</param>
        /// <param name="routeValues">Route values for generating URI</param>
        /// <param name="message">Optional success message</param>
        /// <returns>ActionResult with standardized ApiResponse</returns>
        public static ActionResult<ApiResponse<T>> ApiCreatedAtAction<T>(
            this ControllerBase controller,
            T data,
            string actionName,
            string controllerName,
            object routeValues,
            string message = null)
        {
            var response = ApiResponse<T>.SuccessResponse(data, message ?? "Resource created successfully");
            return controller.CreatedAtAction(actionName, controllerName, routeValues, response);
        }
    }
}