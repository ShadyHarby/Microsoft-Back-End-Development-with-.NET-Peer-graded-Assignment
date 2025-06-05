using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        // In a real application, these would come from a secure configuration
        private readonly HashSet<string> _validTokens = new()
        {
            "api-key-hr-department-2024",
            "api-key-it-department-2024",
            "api-key-admin-2024",
            "demo-token-for-testing"
        };

        // Endpoints that don't require authentication
        private readonly HashSet<string> _publicEndpoints = new()
        {
            "/",
            "/health",
            "/api/docs",
            "/swagger"
        };

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "Unknown";
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Skip authentication for public endpoints
            if (IsPublicEndpoint(path))
            {
                _logger.LogDebug("[{RequestId}] Skipping authentication for public endpoint: {Path}", requestId, path);
                await _next(context);
                return;
            }

            // Skip authentication for development endpoints
            if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() && 
                (path.Contains("/swagger") || path.Contains("/openapi")))
            {
                _logger.LogDebug("[{RequestId}] Skipping authentication for development endpoint: {Path}", requestId, path);
                await _next(context);
                return;
            }

            try
            {
                var token = ExtractToken(context.Request);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[{RequestId}] No authentication token provided for {Path}", requestId, path);
                    await WriteUnauthorizedResponse(context, "Authentication token is required", requestId);
                    return;
                }

                if (!IsValidToken(token))
                {
                    _logger.LogWarning("[{RequestId}] Invalid authentication token provided for {Path}", requestId, path);
                    await WriteUnauthorizedResponse(context, "Invalid authentication token", requestId);
                    return;
                }

                // Add user information to context for downstream middleware/controllers
                context.Items["UserId"] = GetUserIdFromToken(token);
                context.Items["UserRole"] = GetUserRoleFromToken(token);

                _logger.LogDebug("[{RequestId}] Authentication successful for {Path}", requestId, path);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] Error during authentication for {Path}", requestId, path);
                await WriteUnauthorizedResponse(context, "Authentication error occurred", requestId);
            }
        }

        private string? ExtractToken(HttpRequest request)
        {
            // Check Authorization header first (Bearer token)
            var authHeader = request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..].Trim();
            }

            // Check X-API-Key header
            var apiKeyHeader = request.Headers["X-API-Key"].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKeyHeader))
            {
                return apiKeyHeader.Trim();
            }

            // Check query parameter (less secure, but useful for testing)
            var queryToken = request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(queryToken))
            {
                return queryToken.Trim();
            }

            return null;
        }

        private bool IsValidToken(string token)
        {
            // In a real application, this would validate JWT tokens or check against a database
            return _validTokens.Contains(token);
        }

        private string GetUserIdFromToken(string token)
        {
            // In a real application, this would extract user ID from JWT claims
            return token switch
            {
                "api-key-hr-department-2024" => "hr-user",
                "api-key-it-department-2024" => "it-user",
                "api-key-admin-2024" => "admin-user",
                "demo-token-for-testing" => "demo-user",
                _ => "unknown-user"
            };
        }

        private string GetUserRoleFromToken(string token)
        {
            // In a real application, this would extract role from JWT claims
            return token switch
            {
                "api-key-hr-department-2024" => "HR",
                "api-key-it-department-2024" => "IT",
                "api-key-admin-2024" => "Admin",
                "demo-token-for-testing" => "Demo",
                _ => "User"
            };
        }

        private bool IsPublicEndpoint(string path)
        {
            return _publicEndpoints.Any(endpoint => 
                path.Equals(endpoint, StringComparison.OrdinalIgnoreCase) || 
                path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
        }

        private async Task WriteUnauthorizedResponse(HttpContext context, string message, string requestId)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "Unauthorized",
                message,
                requestId,
                timestamp = DateTime.UtcNow
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
} 