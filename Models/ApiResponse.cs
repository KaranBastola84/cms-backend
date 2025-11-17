namespace JWTAuthAPI.Models
{
    public class ApiResponse<T>
    {
        public T? Result { get; set; }
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public List<string> ErrorMessage { get; set; } = new();

        public ApiResponse(T? result = default, bool isSuccess = false, int statusCode = 0, string? message = null)
        {
            Result = result;
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            if (!string.IsNullOrEmpty(message))
                ErrorMessage.Add(message);
        }
    }
}