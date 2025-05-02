// ApiResponse.cs
using System.Text.Json.Serialization; // Dodaj, jeśli chcesz kontrolować serializację np. ignorowanie nulla

namespace LeafLoop.Models.API
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty; // Dobrą praktyką jest inicjalizacja stringów
        public T? Data { get; set; } // Rozważ użycie T? jeśli T może być typem referencyjnym lub strukturą dopuszczającą null
        public int? TotalItems { get; set; }
        public int? TotalPages { get; set; }
        public int? CurrentPage { get; set; }

        // --- DODANA WŁAŚCIWOŚĆ ---
        /// <summary>
        /// Contains additional developer-specific details, like stack traces.
        /// Populated only in Development environment for error responses.
        /// Should be ignored during JSON serialization if null.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Opcjonalnie: Ignoruj w JSON jeśli null
        public object? DevDetails { get; set; }
        // --------------------------

        public static ApiResponse<T> SuccessResponse(T data, string? message = null) // Użyj string? dla nullable message
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Operation successful",
                Data = data
            };
        }

        public static ApiResponse<T> SuccessResponse(T data, int totalItems, int totalPages, int currentPage,
            string? message = null) // Użyj string? dla nullable message
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

        // Zwróć ApiResponse<object> lub dedykowany typ jeśli T nie pasuje do scenariusza błędu
        // Tutaj zakładamy, że ErrorResponse może być typu ApiResponse<T> gdzie T jest default
        public static ApiResponse<T> ErrorResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default // Data będzie default(T), np. null dla typów referencyjnych
            };
        }

        // Możesz też rozważyć stworzenie niegenerycznej klasy bazowej lub osobnej klasy dla błędów,
        // jeśli nie zawsze chcesz zwracać 'Data = default' w przypadku błędu.
        // Na przykład: public static ApiResponse<object> GenericErrorResponse(string message) { ... }
    }
}