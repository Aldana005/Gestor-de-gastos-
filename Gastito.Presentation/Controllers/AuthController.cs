using FluentValidation;
using Gastito.BusinessLogic.Interfaces;
using Gastito.Presentation.Inputs.Auth;
using Gastito.Presentation.Outputs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthManager _authManager;
    private readonly IValidator<LoginInput> _validator;

    public AuthController(IAuthManager _authManager, IValidator<LoginInput> _validator)
    {
        this._authManager = _authManager;
        this._validator = _validator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginInput input)
    {
        var validationResult = await _validator.ValidateAsync(input);
        if (!validationResult.IsValid)
        {
            var problemDetails = new HttpValidationProblemDetails(
                validationResult.ToDictionary())
            {
                Title = "Error de validación",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Uno o más campos presentan errores."
            };
            return BadRequest(problemDetails);
        }

        var result = _authManager.Authenticate(input.Usuario, input.Contrasena);
        if (result == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales incorrectas",
                Detail = "El usuario o la contraseña ingresados no son válidos.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(new LoginOutput
        {
            Token = result.Token,
            ExpiraEnUtc = result.ExpiraEnUtc
        });
    }
}
