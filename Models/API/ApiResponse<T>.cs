using System.Text.Json.Serialization;

namespace LeafLoop.Models.API
{
    /// <summary>
    /// Represents a standard generic API response structure containing a data payload.
    /// Inherits properties from the non-generic ApiResponse.
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    // Można rozważyć dziedziczenie, jeśli chcesz współdzielić logikę/właściwości: public class ApiResponse<T> : ApiResponse
    public class ApiResponse<T> // Na razie bez dziedziczenia dla prostoty
    {
        /// <summary>
        /// Indicates if the operation was successful.
        /// </summary>
        public bool Success { get; set; } // Zachowujemy public set dla metod fabrycznych

        /// <summary>
        /// A user-friendly message describing the result of the operation.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The data payload of the response.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj Data jeśli jest null
        public T? Data { get; set; } // T? pozwala na null dla typów referencyjnych i nullable value types

        /// <summary>
        /// Total number of items available (used for pagination). Null if not applicable.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj jeśli null
        public int? TotalItems { get; set; }

        /// <summary>
        /// Total number of pages available (used for pagination). Null if not applicable.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj jeśli null
        public int? TotalPages { get; set; }

        /// <summary>
        /// The current page number (used for pagination). Null if not applicable.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj jeśli null
        public int? CurrentPage { get; set; }

        /// <summary>
        /// Contains additional developer-specific details. Only populated in Development for errors.
        /// Note: Errors should typically use the non-generic ApiResponse.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? DevDetails { get; set; }

        /// <summary>
        /// Creates a successful API response with data.
        /// </summary>
        /// <param name="data">The data payload.</param>
        /// <param name="message">Optional success message.</param>
        /// <returns>A new successful ApiResponse<T> instance.</returns>
        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Operation successful",
                Data = data
            };
        }

        /// <summary>
        /// Creates a successful API response with paginated data.
        /// </summary>
        /// <param name="data">The data payload for the current page.</param>
        /// <param name="totalItems">Total number of items across all pages.</param>
        /// <param name="totalPages">Total number of pages available.</param>
        /// <param name="currentPage">The current page number.</param>
        /// <param name="message">Optional success message.</param>
        /// <returns>A new successful ApiResponse<T> instance with pagination info.</returns>
        public static ApiResponse<T> SuccessResponse(T data, int totalItems, int totalPages, int currentPage, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Operation successful",
                Data = data,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage
            };
        }

        // Usunięto ErrorResponse - błędy powinny używać niegenerycznego ApiResponse
        // public static ApiResponse<T> ErrorResponse(string message) { ... }
    }
}