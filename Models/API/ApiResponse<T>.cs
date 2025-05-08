using System.Text.Json.Serialization;

namespace LeafLoop.Models.API;

public class ApiResponse<T> // Na razie bez dziedziczenia dla prostoty
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj Data jeśli jest null
    public T? Data { get; set; } // T? pozwala na null dla typów referencyjnych i nullable value types

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj jeśli null
    public int? TotalItems { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj jeśli null
    public int? TotalPages { get; set; }

    /// <summary>
    /// The current page number (used for pagination). Null if not applicable.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ignoruj jeśli null
    public int? CurrentPage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? DevDetails { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Operation successful",
            Data = data
        };
    }

    public static ApiResponse<T> SuccessResponse(T data, int totalItems, int totalPages, int currentPage,
        string? message = null)
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
}