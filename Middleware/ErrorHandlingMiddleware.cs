using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "Unknown";
            
            _logger.LogError(exception,
                "[{RequestId}] An unhandled exception occurred: {ExceptionType} - {Message}",
                requestId,
                exception.GetType().Name,
                exception.Message
            );

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Error = "Invalid request parameters";
                    errorResponse.Message = exception.Message;
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Error = "Invalid operation";
                    errorResponse.Message = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Error = "Unauthorized access";
                    errorResponse.Message = "Authentication required or invalid credentials";
                    break;

                case NotImplementedException:
                    response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    errorResponse.Error = "Feature not implemented";
                    errorResponse.Message = "This feature is not yet implemented";
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.Error = "Request timeout";
                    errorResponse.Message = "The request took too long to process";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Error = "Internal server error";
                    errorResponse.Message = "An unexpected error occurred while processing your request";
                    
                    // Log additional details for internal server errors
                    _logger.LogError(exception,
                        "[{RequestId}] Internal server error details: {StackTrace}",
                        requestId,
                        exception.StackTrace
                    );
                    break;
            }

            // Add correlation information for debugging
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            {
                errorResponse.CorrelationId = correlationId.FirstOrDefault();
                response.Headers["X-Correlation-ID"] = correlationId;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TraceId { get; set; }
    }
} 