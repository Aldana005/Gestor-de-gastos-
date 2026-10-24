using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Models.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gastito.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramBotService _telegramBotService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramController> _logger;

    public const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    public TelegramController(
        ITelegramBotService telegramBotService,
        IConfiguration configuration,
        ILogger<TelegramController> logger)
    {
        _telegramBotService = telegramBotService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TelegramBotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Webhook(
        [FromBody] TelegramUpdate update,
        [FromHeader(Name = SecretTokenHeader)] string? secretToken,
        CancellationToken ct)
    {
        // Validación del header secreto X-Telegram-Bot-Api-Secret-Token (Sección 11)
        var expectedSecret = _configuration["Telegram:SecretToken"];
        if (!string.IsNullOrWhiteSpace(expectedSecret))
        {
            if (string.IsNullOrWhiteSpace(secretToken) || secretToken != expectedSecret)
            {
                _logger.LogWarning("Intento de acceso a Telegram webhook sin token secreto válido.");
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "No autorizado",
                    Detail = "Token secreto de Telegram no válido o ausente."
                });
            }
        }

        var response = await _telegramBotService.ProcessUpdateAsync(update, ct);
        return Ok(response);
    }
}
