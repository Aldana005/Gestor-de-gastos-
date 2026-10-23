using FluentValidation;
using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Presentation.Inputs.Categories;
using Gastito.Presentation.Mappers;
using Gastito.Presentation.Outputs.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoryManager _categoryManager;
    private readonly IValidator<CategoryCreateInput> _createValidator;
    private readonly IValidator<CategoryUpdateInput> _updateValidator;

    public CategoriasController(
        ICategoryManager categoryManager,
        IValidator<CategoryCreateInput> createValidator,
        IValidator<CategoryUpdateInput> updateValidator)
    {
        _categoryManager = categoryManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryOutput>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] TipoMovimiento? tipo, CancellationToken ct)
    {
        var categories = await _categoryManager.GetAllAsync(tipo, ct);
        return Ok(categories.ToOutputList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var category = await _categoryManager.GetByIdAsync(id, ct);
        if (category == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Categoría no encontrada",
                Detail = $"No se encontró la categoría con ID {id}."
            });
        }

        return Ok(category.ToOutput());
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryOutput), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CategoryCreateInput input, CancellationToken ct)
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
            var created = await _categoryManager.CreateAsync(input.Nombre, input.Tipo, ct);
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
    [ProducesResponseType(typeof(CategoryOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateInput input, CancellationToken ct)
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
            var updated = await _categoryManager.UpdateAsync(id, input.Nombre, ct);
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
            await _categoryManager.DeleteAsync(id, ct);
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
