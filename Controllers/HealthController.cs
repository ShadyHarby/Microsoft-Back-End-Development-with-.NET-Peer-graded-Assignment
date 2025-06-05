using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Diagnostics;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Check API health status
        /// </summary>
        /// <returns>Health status information</returns>
        [HttpGet]
        public ActionResult GetHealthStatus()
        {
            try
            {
                var healthInfo = new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processorCount = Environment.ProcessorCount
                };

                return Ok(healthInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during health check");
                return StatusCode(500, new { status = "Unhealthy", error = "Health check failed" });
            }
        }
    }
} 