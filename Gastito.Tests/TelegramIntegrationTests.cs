using System.Net;
using System.Net.Http.Json;
using Gastito.BusinessLogic.Models.Telegram;
using Gastito.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Gastito.Tests;

public class TelegramIntegrationTests : IClassFixture<GastitoWebApplicationFactory>
{
    private readonly GastitoWebApplicationFactory _factory;
    private const string SecretToken = "GastitoTelegramSecret_2026_SecureToken";

    public TelegramIntegrationTests(GastitoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Webhook_WithoutSecretToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var update = new TelegramUpdate
        {
            UpdateId = 1,
            Message = new TelegramMessage
            {
                MessageId = 10,
                Chat = new TelegramChat { Id = 123456789 },
                Text = "ingreso 10000 Ingreso Principal"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/webhook", update);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("No autorizado", problem.Title);
    }

    [Fact]
    public async Task Webhook_WithInvalidSecretToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TelegramController.SecretTokenHeader, "TokenInvalido123");

        var update = new TelegramUpdate
        {
            UpdateId = 2,
            Message = new TelegramMessage
            {
                MessageId = 11,
                Chat = new TelegramChat { Id = 123456789 },
                Text = "ingreso 10000 Ingreso Principal"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/webhook", update);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WithValidSecretToken_ReturnsOkAndProcessesTransaction()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TelegramController.SecretTokenHeader, SecretToken);

        var update = new TelegramUpdate
        {
            UpdateId = 3,
            Message = new TelegramMessage
            {
                MessageId = 12,
                Chat = new TelegramChat { Id = 123456789 },
                Text = "ingreso 45000 Ingreso Principal"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/telegram/webhook", update);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Content: {content}");
        var botResponse = await response.Content.ReadFromJsonAsync<TelegramBotResponse>();
        Assert.NotNull(botResponse);
        Assert.False(botResponse.Ignorado);
        Assert.Contains("registrado con éxito", botResponse.Texto);
        Assert.NotNull(botResponse.TransaccionCreada);
        Assert.Equal(45000m, botResponse.TransaccionCreada.Monto);
    }
}
