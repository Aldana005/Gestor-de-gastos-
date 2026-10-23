using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "GASTITO API",
            timestampUtc = DateTime.UtcNow
        });
    }
}
