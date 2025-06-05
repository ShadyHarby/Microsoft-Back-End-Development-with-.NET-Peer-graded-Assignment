using System.Diagnostics;

namespace UserManagementAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            // Add request ID to context for tracking
            context.Items["RequestId"] = requestId;

            try
            {
                // Log incoming request
                _logger.LogInformation(
                    "[{RequestId}] Incoming {Method} request to {Path} from {RemoteIpAddress}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                // Log request headers (excluding sensitive data)
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var relevantHeaders = context.Request.Headers
                        .Where(h => !IssensitiveHeader(h.Key))
                        .ToDictionary(h => h.Key, h => h.Value.ToString());
                    
                    _logger.LogDebug(
                        "[{RequestId}] Request headers: {@Headers}",
                        requestId,
                        relevantHeaders
                    );
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{RequestId}] Unhandled exception occurred during request processing",
                    requestId
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Log response details
                _logger.LogInformation(
                    "[{RequestId}] Completed {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );

                // Log slow requests as warnings
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning(
                        "[{RequestId}] Slow request detected: {Method} {Path} took {ElapsedMs}ms",
                        requestId,
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds
                    );
                }
            }
        }

        private static bool IssensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[]
            {
                "authorization",
                "cookie",
                "x-api-key",
                "x-auth-token",
                "authentication"
            };

            return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
        }
    }
} 