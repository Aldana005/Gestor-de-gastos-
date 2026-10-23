using FluentValidation;
using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Mappers;
using Gastito.Presentation.Outputs.Common;
using Gastito.Presentation.Outputs.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class MovimientosController : ControllerBase
{
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<TransactionCreateInput> _createValidator;
    private readonly IValidator<TransactionUpdateInput> _updateValidator;

    public MovimientosController(
        ITransactionManager transactionManager,
        IValidator<TransactionCreateInput> createValidator,
        IValidator<TransactionUpdateInput> updateValidator)
    {
        _transactionManager = transactionManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionOutput>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] TransactionFilterInput filter, CancellationToken ct)
    {
        var (items, totalCount) = await _transactionManager.GetPagedAsync(
            filter.Desde,
            filter.Hasta,
            filter.Tipo,
            filter.CategoriaIds,
            filter.Pagina,
            filter.TamanoPagina,
            ct);

        return Ok(items.ToPagedResult(totalCount, filter.Pagina, filter.TamanoPagina));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var transaction = await _transactionManager.GetByIdAsync(id, ct);
        if (transaction == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Movimiento no encontrado",
                Detail = $"No se encontró el movimiento con ID {id}."
            });
        }

        return Ok(transaction.ToOutput());
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] TransactionCreateInput input, CancellationToken ct)
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
            var created = await _transactionManager.CreateAsync(input.ToEntity(), ct);
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
    [ProducesResponseType(typeof(TransactionOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] TransactionUpdateInput input, CancellationToken ct)
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

        var existing = await _transactionManager.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Movimiento no encontrado",
                Detail = $"No se encontró el movimiento con ID {id}."
            });
        }

        try
        {
            var updated = await _transactionManager.UpdateAsync(id, input.ToEntity(), ct);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await _transactionManager.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Movimiento no encontrado",
                Detail = $"No se encontró el movimiento con ID {id}."
            });
        }

        try
        {
            await _transactionManager.DeleteAsync(id, ct);
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
