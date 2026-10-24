using System.Collections.Concurrent;
using Gastito.BusinessLogic.Interfaces;
using Gastito.BusinessLogic.Models.Telegram;
using Gastito.BusinessLogic.Services;
using Gastito.DataAccess.Interfaces;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gastito.Tests;

public class TelegramBotServiceTests
{
    private readonly Mock<ITransactionManager> _transactionManagerMock = new();
    private readonly Mock<ICategoryStore> _categoryStoreMock = new();
    private readonly Mock<ITarjetaStore> _tarjetaStoreMock = new();
    private readonly Mock<ITelegramConversacionStore> _conversacionStoreMock = new();
    private readonly Mock<ILogger<TelegramBotService>> _loggerMock = new();

    private const long AuthorizedChatId = 123456789;

    private TelegramBotService CreateService(string? authorizedChatId = "123456789")
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Telegram:AuthorizedChatId", authorizedChatId },
            { "Telegram:SecretToken", "TestSecretToken" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return new TelegramBotService(
            _transactionManagerMock.Object,
            _categoryStoreMock.Object,
            _tarjetaStoreMock.Object,
            _conversacionStoreMock.Object,
            config,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessUpdate_WhenUnauthorizedChatId_IgnoresUpdate()
    {
        // Arrange
        var service = CreateService();
        var update = new TelegramUpdate
        {
            UpdateId = 1,
            Message = new TelegramMessage
            {
                MessageId = 10,
                Chat = new TelegramChat { Id = 999999999 }, // Unauthorized
                Text = "gasto 1000 Supermercado"
            }
        };

        // Act
        var result = await service.ProcessUpdateAsync(update);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Ignorado);
        _transactionManagerMock.Verify(t => t.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessUpdate_WithHelpCommand_ReturnsHelpMessage()
    {
        // Arrange
        var service = CreateService();
        var update = new TelegramUpdate
        {
            UpdateId = 1,
            Message = new TelegramMessage
            {
                MessageId = 11,
                Chat = new TelegramChat { Id = AuthorizedChatId },
                Text = "/help"
            }
        };

        // Act
        var result = await service.ProcessUpdateAsync(update);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Ignorado);
        Assert.Contains("GASTITO Bot", result.Texto);
        Assert.Contains("ingreso", result.Texto);
        Assert.Contains("gasto", result.Texto);
    }

    [Fact]
    public async Task ProcessUpdate_DirectIngreso_CreatesTransaction()
    {
        // Arrange
        var service = CreateService();
        var category = new Category { Id = 4, Nombre = "Ingreso Principal", Tipo = TipoMovimiento.Ingreso };

        _categoryStoreMock.Setup(c => c.GetAllAsync(TipoMovimiento.Ingreso, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { category });

        _transactionManagerMock.Setup(m => m.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => { t.Id = 100; return t; });

        var update = new TelegramUpdate
        {
            UpdateId = 2,
            Message = new TelegramMessage
            {
                MessageId = 12,
                Chat = new TelegramChat { Id = AuthorizedChatId },
                Text = "ingreso 50000 Ingreso Principal"
            }
        };

        // Act
        var result = await service.ProcessUpdateAsync(update);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Ignorado);
        Assert.NotNull(result.TransaccionCreada);
        Assert.Equal(50000m, result.TransaccionCreada.Monto);
        Assert.Equal("Ingreso", result.TransaccionCreada.Tipo);
        Assert.Equal(4, result.TransaccionCreada.CategoryId);
        Assert.Contains("registrado con éxito", result.Texto);
    }

    [Fact]
    public async Task ProcessUpdate_DirectGastoWithDashes_CreatesTransaction()
    {
        // Arrange
        var service = CreateService();
        var category = new Category { Id = 1, Nombre = "Supermercado", Tipo = TipoMovimiento.Gasto };

        _categoryStoreMock.Setup(c => c.GetAllAsync(TipoMovimiento.Gasto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { category });

        _transactionManagerMock.Setup(m => m.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => { t.Id = 101; return t; });

        var update = new TelegramUpdate
        {
            UpdateId = 3,
            Message = new TelegramMessage
            {
                MessageId = 13,
                Chat = new TelegramChat { Id = AuthorizedChatId },
                Text = "gasto - 12500 - Supermercado"
            }
        };

        // Act
        var result = await service.ProcessUpdateAsync(update);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Ignorado);
        Assert.NotNull(result.TransaccionCreada);
        Assert.Equal(12500m, result.TransaccionCreada.Monto);
        Assert.Equal("Gasto", result.TransaccionCreada.Tipo);
        Assert.Equal("Efectivo", result.TransaccionCreada.MedioPago);
    }

    [Fact]
    public async Task ProcessUpdate_DirectGastoInCuotas_CreatesCuotasTransaction()
    {
        // Arrange
        var service = CreateService();
        var category = new Category { Id = 2, Nombre = "Hogar", Tipo = TipoMovimiento.Gasto };
        var tarjeta = new Tarjeta { Id = 1, Nombre = "Visa Santander", TipoTarjeta = TipoTarjeta.Credito };

        _categoryStoreMock.Setup(c => c.GetAllAsync(TipoMovimiento.Gasto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { category });
        _tarjetaStoreMock.Setup(t => t.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tarjeta> { tarjeta });

        _transactionManagerMock.Setup(m => m.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => { t.Id = 102; return t; });

        var update = new TelegramUpdate
        {
            UpdateId = 4,
            Message = new TelegramMessage
            {
                MessageId = 14,
                Chat = new TelegramChat { Id = AuthorizedChatId },
                Text = "gasto 120000 Heladera 6"
            }
        };

        // Act
        var result = await service.ProcessUpdateAsync(update);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Ignorado);
        Assert.NotNull(result.TransaccionCreada);
        Assert.Equal(120000m, result.TransaccionCreada.Monto);
        Assert.Equal(6, result.TransaccionCreada.CantidadCuotas);
        Assert.Equal("Tarjeta", result.TransaccionCreada.MedioPago);
        Assert.Equal(1, result.TransaccionCreada.TarjetaId);
        Assert.Contains("cuotas registrada con éxito", result.Texto);
    }

    [Fact]
    public async Task ProcessUpdate_NuevoCommand_StartsConversationalFlow()
    {
        // Arrange
        var service = CreateService();
        var conv = new TelegramConversacion { ChatId = AuthorizedChatId };

        _conversacionStoreMock.Setup(c => c.GetOrCreateAsync(AuthorizedChatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv);

        var update = new TelegramUpdate
        {
            UpdateId = 5,
            Message = new TelegramMessage
            {
                MessageId = 15,
                Chat = new TelegramChat { Id = AuthorizedChatId },
                Text = "/nuevo"
            }
        };

        // Act
        var result = await service.ProcessUpdateAsync(update);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Ignorado);
        Assert.Contains("tipo de movimiento", result.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.InlineKeyboard);
        Assert.NotEmpty(result.InlineKeyboard);
        Assert.Equal(TelegramEstadoConversacion.EsperandoTipo, conv.Estado);
    }
}
