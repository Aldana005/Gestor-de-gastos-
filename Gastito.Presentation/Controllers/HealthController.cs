using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "GASTITO API",
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult GetProtected()
    {
        return Ok(new
        {
            status = "Authorized",
            user = User.Identity?.Name,
            timestampUtc = DateTime.UtcNow
        });
    }
}
