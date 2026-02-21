using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace JWTAuthAPI.Middleware
{
    public class RateLimitingMiddleware : IMiddleware
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // Configuration: requests per minute for different endpoint types
        private readonly Dictionary<string, int> _rateLimits = new()
        {
            { "/api/auth/login", 5 },              // 5 login attempts per minute
            { "/api/order", 10 },                   // 10 orders per minute (public)
            { "/api/productreview", 10 },           // 10 reviews per minute
            { "/api/inquiry", 10 },                 // 10 inquiries per minute
            { "/api/staffmanagement/send-otp", 3 }, // 3 OTP requests per minute
            { "/api/trainermanagement/send-otp", 3 } // 3 OTP requests per minute
        };

        private const int DefaultRateLimit = 100; // Default: 100 requests per minute
        private const int BlockDurationMinutes = 5; // Block for 5 minutes after limit exceeded

        public RateLimitingMiddleware(IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // Get client identifier (IP address + endpoint)
            var clientId = GetClientIdentifier(context);
            var endpoint = GetEndpointKey(context.Request.Path.Value?.ToLower() ?? "");

            // Check if client is temporarily blocked
            var blockKey = $"blocked_{clientId}_{endpoint}";
            if (_cache.TryGetValue(blockKey, out _))
            {
                _logger.LogWarning("Rate limit exceeded for {ClientId} on {Endpoint}", clientId, endpoint);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = (BlockDurationMinutes * 60).ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429,
                    isSuccess = false,
                    errorMessage = new[] { $"Too many requests. Please try again after {BlockDurationMinutes} minutes." }
                });
                return;
            }

            // Get rate limit for this endpoint
            var limit = GetRateLimit(endpoint);

            // Track request count
            var cacheKey = $"ratelimit_{clientId}_{endpoint}";
            var requestCount = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            requestCount++;
            _cache.Set(cacheKey, requestCount, TimeSpan.FromMinutes(1));

            // Check if limit exceeded
            if (requestCount > limit)
            {
                // Block the client temporarily
                _cache.Set(blockKey, true, TimeSpan.FromMinutes(BlockDurationMinutes));

                _logger.LogWarning("Rate limit exceeded for {ClientId} on {Endpoint}. Blocked for {Minutes} minutes.",
                    clientId, endpoint, BlockDurationMinutes);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = (BlockDurationMinutes * 60).ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429,
                    isSuccess = false,
                    errorMessage = new[] { $"Rate limit exceeded. Access blocked for {BlockDurationMinutes} minutes." }
                });
                return;
            }

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - requestCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();

            await next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Try to get real IP from proxy headers first
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private string GetEndpointKey(string path)
        {
            // Normalize endpoint path for rate limiting
            foreach (var endpoint in _rateLimits.Keys)
            {
                if (path.StartsWith(endpoint))
                {
                    return endpoint;
                }
            }
            return "default";
        }

        private int GetRateLimit(string endpoint)
        {
            return _rateLimits.TryGetValue(endpoint, out var limit) ? limit : DefaultRateLimit;
        }
    }
}
