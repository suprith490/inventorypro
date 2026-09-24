using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Health;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Simple controller used to verify that the API is running.
/// Mirrors a Spring Boot @RestController with @RequestMapping("/api/health").
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<HealthStatusDto>> Get()
    {
        var health = new HealthStatusDto
        {
            Status = "Healthy",
            Application = "InventoryPro API",
            Version = "v1",
            TimestampUtc = DateTime.UtcNow
        };

        return Ok(ApiResponse<HealthStatusDto>.Ok(health, "API is healthy."));
    }
}
