using FluentValidation;
using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.Presentation.Inputs.Tarjetas;
using Gastito.Presentation.Mappers;
using Gastito.Presentation.Outputs.Tarjetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class TarjetasController : ControllerBase
{
    private readonly ITarjetaManager _tarjetaManager;
    private readonly IValidator<TarjetaCreateInput> _createValidator;
    private readonly IValidator<TarjetaUpdateInput> _updateValidator;

    public TarjetasController(
        ITarjetaManager tarjetaManager,
        IValidator<TarjetaCreateInput> createValidator,
        IValidator<TarjetaUpdateInput> updateValidator)
    {
        _tarjetaManager = tarjetaManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TarjetaOutput>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tarjetas = await _tarjetaManager.GetAllAsync(ct);
        return Ok(tarjetas.ToOutputList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TarjetaOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var tarjeta = await _tarjetaManager.GetByIdAsync(id, ct);
        if (tarjeta == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tarjeta no encontrada",
                Detail = $"No se encontró la tarjeta con ID {id}."
            });
        }

        return Ok(tarjeta.ToOutput());
    }

    [HttpPost]
    [ProducesResponseType(typeof(TarjetaOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] TarjetaCreateInput input, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new HttpValidationProblemDetails(validation.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación"
            });
        }

        try
        {
            var created = await _tarjetaManager.CreateAsync(input.Nombre, input.TipoTarjeta, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToOutput());
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Regla de negocio no cumplida",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TarjetaOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] TarjetaUpdateInput input, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new HttpValidationProblemDetails(validation.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación"
            });
        }

        try
        {
            var updated = await _tarjetaManager.UpdateAsync(id, input.Nombre, input.TipoTarjeta, ct);
            return Ok(updated.ToOutput());
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Regla de negocio no cumplida",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _tarjetaManager.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Regla de negocio no cumplida",
                Detail = ex.Message
            });
        }
    }
}
