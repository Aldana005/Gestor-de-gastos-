using FluentValidation.TestHelper;
using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.BusinessLogic.Utils;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Presentation.Controllers;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Outputs.Transactions;
using Gastito.Presentation.Validators.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class FijosTests : IDisposable
{
    private readonly TransactionCreateInputValidator _createValidator = new();
    private readonly TransactionUpdateInputValidator _updateValidator = new();

    public FijosTests()
    {
        // Limpiar cualquier mock de reloj
        DateTimeUtils.ResetClock();
    }

    public void Dispose()
    {
        DateTimeUtils.ResetClock();
    }

    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateAsync_Fijo_InitializesFechaInicioAndCreatesHistorialMonto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var transaction = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 400000m,
            Fecha = new DateTime(2026, 3, 1),
            Descripcion = "Alquiler departamento",
            CategoryId = 2, // Hogar (Gasto del seed)
            MedioPago = MedioPago.Transferencia
        };

        // Act
        var created = await manager.CreateAsync(transaction);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(Frecuencia.Fijo, created.Frecuencia);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), created.FechaInicio);
        Assert.Null(created.FechaFin);

        // Verificar HistorialMonto inicial
        Assert.Single(created.HistorialMontos);
        var historial = created.HistorialMontos.First();
        Assert.Equal(400000m, historial.Monto);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), historial.VigenteDesde);
    }

    [Fact]
    public async Task CreateAsync_Fijo_WhenFechaFinIsBeforeFechaInicio_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var transaction = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 15000m,
            FechaInicio = new DateTime(2026, 6, 1),
            FechaFin = new DateTime(2026, 5, 31), // Anterior a FechaInicio
            Descripcion = "Suscripción",
            CategoryId = 2, // Hogar
            MedioPago = MedioPago.Transferencia
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.CreateAsync(transaction));
        Assert.Equal("La fecha de fin no puede ser anterior a la fecha de inicio.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Fijo_WhenHasCuotas_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var transaction = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 50000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 1, 1),
            Descripcion = "Fijo inválido con cuotas",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.CreateAsync(transaction));
        Assert.Equal("Los movimientos fijos no pueden tener cuotas.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Fijo_AplicarDesdeMesActual_UpdatesMontoAndHistory()
    {
        // Arrange: Simular que hoy es Junio 2026
        DateTimeUtils.SetClock(() => new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var transaction = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 400000m,
            Fecha = new DateTime(2026, 3, 1),
            Descripcion = "Alquiler",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        });

        // Act: En Junio sube a 450.000 con MesActual
        var updateData = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 450000m,
            Fecha = transaction.Fecha,
            Descripcion = "Alquiler ajustado",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        };

        var updated = await manager.UpdateAsync(transaction.Id, updateData, AplicarDesde.MesActual);

        // Assert
        Assert.Equal(450000m, updated.Monto);
        Assert.Equal(2, updated.HistorialMontos.Count);

        // Historial marzo: 400.000
        var histMarzo = updated.HistorialMontos.FirstOrDefault(h => h.VigenteDesde == new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotNull(histMarzo);
        Assert.Equal(400000m, histMarzo.Monto);

        // Historial junio: 450.000
        var histJunio = updated.HistorialMontos.FirstOrDefault(h => h.VigenteDesde == new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotNull(histJunio);
        Assert.Equal(450000m, histJunio.Monto);
    }

    [Fact]
    public async Task UpdateAsync_Fijo_AplicarDesdeMesSiguiente_RegistersForNextMonth()
    {
        // Arrange: Simular que hoy es Junio 2026
        DateTimeUtils.SetClock(() => new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var transaction = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 400000m,
            Fecha = new DateTime(2026, 3, 1),
            Descripcion = "Alquiler",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        });

        // Act: En Junio se define aumento a 450.000 desde MesSiguiente (Julio)
        var updateData = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 450000m,
            Fecha = transaction.Fecha,
            Descripcion = "Alquiler ajustado prox mes",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        };

        var updated = await manager.UpdateAsync(transaction.Id, updateData, AplicarDesde.MesSiguiente);

        // Assert
        // Debe haber registro para Julio 2026 con 450.000
        var histJulio = updated.HistorialMontos.FirstOrDefault(h => h.VigenteDesde == new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotNull(histJulio);
        Assert.Equal(450000m, histJulio.Monto);

        // Mes actual (junio) conserva 400.000
        var histJunio = updated.HistorialMontos.FirstOrDefault(h => h.VigenteDesde == new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotNull(histJunio);
        Assert.Equal(400000m, histJunio.Monto);
    }

    [Fact]
    public async Task GetPagedAsync_Fijo_GeneratesOneOccurrencePerMonthWithCorrectHistoricalMontos()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        // Alquiler creado en Marzo 2026 a $400.000
        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 400000m,
            Fecha = new DateTime(2026, 3, 5),
            FechaInicio = new DateTime(2026, 3, 5),
            Descripcion = "Alquiler",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        });

        // En Junio 2026 sube a 450.000 (MesActual)
        DateTimeUtils.SetClock(() => new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc));
        await manager.UpdateAsync(trans.Id, new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 450000m,
            Fecha = trans.Fecha,
            Descripcion = "Alquiler",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        }, AplicarDesde.MesActual);

        // Act: Consultar desde 01/03/2026 hasta 31/07/2026 (5 meses: Marzo, Abril, Mayo, Junio, Julio)
        var (items, totalCount) = await manager.GetPagedAsync(
            desde: new DateTime(2026, 3, 1),
            hasta: new DateTime(2026, 7, 31),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 10);

        // Assert
        Assert.Equal(5, totalCount);
        Assert.Equal(5, items.Count);

        // Están ordenados descendente por fecha:
        // items[0] -> Julio: $450.000
        Assert.Equal(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc), items[0].Fecha);
        Assert.Equal(450000m, items[0].Monto);

        // items[1] -> Junio: $450.000
        Assert.Equal(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), items[1].Fecha);
        Assert.Equal(450000m, items[1].Monto);

        // items[2] -> Mayo: $400.000
        Assert.Equal(new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc), items[2].Fecha);
        Assert.Equal(400000m, items[2].Monto);

        // items[3] -> Abril: $400.000
        Assert.Equal(new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), items[3].Fecha);
        Assert.Equal(400000m, items[3].Monto);

        // items[4] -> Marzo: $400.000
        Assert.Equal(new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), items[4].Fecha);
        Assert.Equal(400000m, items[4].Monto);
    }

    [Fact]
    public void Validator_TransactionCreateInput_ValidatesFechaFinNotBeforeFechaInicio()
    {
        // Arrange
        var inputInvalido = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 1000m,
            FechaInicio = new DateTime(2026, 5, 1),
            FechaFin = new DateTime(2026, 4, 30),
            Descripcion = "Gasto fijo inválido",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };

        var inputValido = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 1000m,
            FechaInicio = new DateTime(2026, 5, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Descripcion = "Gasto fijo válido",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };

        // Act & Assert
        var resultInvalido = _createValidator.TestValidate(inputInvalido);
        resultInvalido.ShouldHaveValidationErrorFor(x => x.FechaFin)
            .WithErrorMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");

        var resultValido = _createValidator.TestValidate(inputValido);
        resultValido.ShouldNotHaveValidationErrorFor(x => x.FechaFin);
    }

    [Fact]
    public async Task MovimientosController_Update_WithQueryParamAplicarDesde_ReturnsOk()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var controller = new MovimientosController(manager, _createValidator, _updateValidator);

        var created = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 200000m,
            Fecha = new DateTime(2026, 1, 1),
            Descripcion = "Gimnasio",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        });

        var updateInput = new TransactionUpdateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 250000m,
            Fecha = created.Fecha,
            Descripcion = "Gimnasio Pase VIP",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        };

        // Act
        var result = await controller.Update(created.Id, updateInput, AplicarDesde.MesSiguiente);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var output = Assert.IsType<TransactionOutput>(okResult.Value);
        Assert.NotNull(output.HistorialMontos);
        Assert.True(output.HistorialMontos.Count >= 2);
    }
}
