using Gastito.BusinessLogic.Interfaces;
using Gastito.Presentation.Mappers;
using Gastito.Presentation.Outputs.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reportes")]
public class ReportesController : ControllerBase
{
    private readonly IReportesManager _reportesManager;

    public ReportesController(IReportesManager reportesManager)
    {
        _reportesManager = reportesManager;
    }

    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ReporteResumenOutput), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ReporteResumenOutput>> GetResumen(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] List<int>? categoriaIds,
        CancellationToken ct = default)
    {
        var result = await _reportesManager.GetResumenAsync(desde, hasta, categoriaIds, ct);
        return Ok(result.ToOutput());
    }
}
