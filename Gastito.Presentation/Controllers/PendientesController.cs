using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Utils;
using Gastito.Presentation.Inputs.Cuotas;
using Gastito.Presentation.Mappers;
using Gastito.Presentation.Outputs.Cuotas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class PendientesController : ControllerBase
{
    private readonly ITransactionManager _transactionManager;

    public PendientesController(ITransactionManager transactionManager)
    {
        _transactionManager = transactionManager;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PendienteItemOutput>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendientes([FromQuery] PendientesFilterInput filter, CancellationToken ct)
    {
        var today = DateTimeUtils.GetArgentinaToday();
        var desde = filter.Desde ?? DateTimeUtils.GetStartOfMonthUtc(today);
        var hasta = filter.Hasta ?? new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month), 23, 59, 59, DateTimeKind.Utc);

        var items = await _transactionManager.GetPendientesAsync(
            desde,
            hasta,
            filter.CategoriaIds,
            filter.MontoMin,
            filter.MontoMax,
            ct);

        return Ok(items.ToPendienteOutputList(desde, hasta));
    }
}
