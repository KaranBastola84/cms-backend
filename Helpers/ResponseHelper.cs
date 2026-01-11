using JWTAuthAPI.Models;

namespace JWTAuthAPI.Helpers
{
    public static class ResponseHelper
    {
        public static ApiResponse<T> Success<T>(T result, string message = "Success", int statusCode = 200)
            => new(result, true, statusCode, message);

        public static ApiResponse<T> Error<T>(string message, int statusCode = 400)
            => new(default, false, statusCode, message);
    }
}