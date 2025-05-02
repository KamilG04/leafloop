using System.Text.Json.Serialization; // Potrzebne dla JsonIgnore

namespace LeafLoop.Models.API
{
    /// <summary>
    /// Represents a standard, non-generic API response structure, typically used for
    /// success messages without data payload or for error responses.
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Indicates if the operation was successful.
        /// </summary>
        public bool Success { get; protected set; } // Użyj protected set, aby ustawiać tylko w metodach fabrycznych

        /// <summary>
        /// A user-friendly message describing the result of the operation.
        /// </summary>
        public string Message { get; protected set; } = string.Empty;

        /// <summary>
        /// Contains specific error details, such as validation errors.
        /// Null if there are no errors or if Success is true.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; protected set; } // Nullable object for flexibility

        /// <summary>
        /// Contains additional developer-specific details (e.g., stack trace).
        /// Populated only in Development environment for error responses.
        /// Should be ignored during JSON serialization if null.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? DevDetails { get; protected set; } // Nullable object

        // Prywatny konstruktor, aby wymusić użycie metod fabrycznych
        protected ApiResponse() { }

        /// <summary>
        /// Creates a successful API response instance.
        /// </summary>
        /// <param name="message">Optional success message.</param>
        /// <returns>A new successful ApiResponse instance.</returns>
        public static ApiResponse SuccessResponse(string? message = null)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message ?? "Operation successful."
            };
        }

        /// <summary>
        /// Creates an error API response instance.
        /// </summary>
        /// <param name="message">The primary error message.</param>
        /// <param name="errors">Optional specific error details (e.g., validation errors).</param>
        /// <param name="devDetails">Optional developer details (only populated in Development).</param>
        /// <returns>A new error ApiResponse instance.</returns>
        public static ApiResponse ErrorResponse(string message, object? errors = null, object? devDetails = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                DevDetails = devDetails
            };
        }
    }
}