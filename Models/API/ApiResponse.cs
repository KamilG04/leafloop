// ApiResponse.cs

namespace LeafLoop.Models.API
{


    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public int? TotalItems { get; set; }
        public int? TotalPages { get; set; }
        public int? CurrentPage { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message ?? "Operation successful",
                Data = data
            };
        }

        public static ApiResponse<T> SuccessResponse(T data, int totalItems, int totalPages, int currentPage,
            string message = null)
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

        public static ApiResponse<T> ErrorResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}