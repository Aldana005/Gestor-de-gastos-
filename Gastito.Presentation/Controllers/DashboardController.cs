using Gastito.BusinessLogic.Interfaces;
using Gastito.Presentation.Mappers;
using Gastito.Presentation.Outputs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardManager _dashboardManager;

    public DashboardController(IDashboardManager dashboardManager)
    {
        _dashboardManager = dashboardManager;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardOutput), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<DashboardOutput>> GetDashboard(
        [FromQuery] int meses = 3,
        CancellationToken ct = default)
    {
        var result = await _dashboardManager.GetDashboardAsync(meses, ct);
        return Ok(result.ToOutput());
    }
}
