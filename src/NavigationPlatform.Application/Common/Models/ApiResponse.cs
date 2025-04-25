namespace NavigationPlatform.Application.Common.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponse(bool success, string message, T data)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static ApiResponse<T> CreateSuccess(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>(true, message, data);
        }

        public static ApiResponse<T> CreateFailure(string message, T data = default)
        {
            return new ApiResponse<T>(false, message, data);
        }
    }

    // For responses without data
    public class ApiResponse : ApiResponse<object>
    {
        public ApiResponse(bool success, string message) : base(success, message, null)
        {
        }

        public static ApiResponse CreateSuccess(string message = "Operation completed successfully")
        {
            return new ApiResponse(true, message);
        }

        public static ApiResponse CreateFailure(string message)
        {
            return new ApiResponse(false, message);
        }
    }
} 