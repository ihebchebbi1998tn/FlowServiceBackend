namespace MyApi.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
<<<<<<< HEAD
        public IEnumerable<string>? Errors { get; set; }

        // Factory helpers for convenience and consistency
        public static ApiResponse<T> SuccessResponse(T? data, string message = "")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
=======
        public IEnumerable<string>? Errors { get; set; }
>>>>>>> 629019b25a1b24c2ca1541c01f196785fde876af
    }
}
