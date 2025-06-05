using UserManagementAPI.Services;
using UserManagementAPI.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add API Explorer services for OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register application services
builder.Services.AddScoped<IUserService, UserService>();

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add CORS (useful for web applications calling the API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline in the correct order:
// 1. Error handling middleware (first to catch all exceptions)
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2. HTTPS redirection
app.UseHttpsRedirection();

// 3. CORS (if needed)
app.UseCors("AllowSpecificOrigins");

// 4. Authentication middleware (validate tokens)
app.UseMiddleware<AuthenticationMiddleware>();

// 5. Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "User Management API v1");
        c.RoutePrefix = "swagger";
    });
}

// 6. Request logging middleware (last to log everything)
app.UseMiddleware<RequestLoggingMiddleware>();

// 7. Configure routing
app.UseRouting();

// 8. Map controllers
app.MapControllers();

// 9. Map health checks
app.MapHealthChecks("/health");

// 10. Add a simple root endpoint
app.MapGet("/", () => new
{
    name = "User Management API",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
    description = "A comprehensive API for managing users with CRUD operations, authentication, and logging",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    documentation = "/swagger",
    healthCheck = "/health",
    endpoints = new
    {
        users = "/api/users",
        health = "/api/health"
    },
    authentication = new
    {
        note = "API requires authentication for all endpoints except health checks and documentation",
        methods = new[] { "Bearer token", "X-API-Key header", "token query parameter" },
        validTokens = new[]
        {
            "api-key-hr-department-2024",
            "api-key-it-department-2024", 
            "api-key-admin-2024",
            "demo-token-for-testing"
        }
    }
}).WithName("GetApiInfo").AllowAnonymous();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting User Management API");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("API Documentation available at: /swagger");
logger.LogInformation("Health checks available at: /health");

app.Run();
