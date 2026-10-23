using FluentValidation.TestHelper;
using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Entities.Utils;
using Gastito.Presentation.Controllers;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Outputs.Transactions;
using Gastito.Presentation.Validators.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class CuotasTests
{
    private readonly TransactionCreateInputValidator _createValidator = new();
    private readonly TransactionUpdateInputValidator _updateValidator = new();

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
    public void CuotaCalculator_CalcularValorCuota_AbsorbsRoundingOnLastCuota()
    {
        // Caso $100.000 en 3 cuotas: 33.333,33 + 33.333,33 + 33.333,34 = 100.000
        decimal total = 100000m;
        int cuotas = 3;

        var c1 = CuotaCalculator.CalcularValorCuota(total, cuotas, 1);
        var c2 = CuotaCalculator.CalcularValorCuota(total, cuotas, 2);
        var c3 = CuotaCalculator.CalcularValorCuota(total, cuotas, 3);

        Assert.Equal(33333.33m, c1);
        Assert.Equal(33333.33m, c2);
        Assert.Equal(33333.34m, c3);
        Assert.Equal(total, c1 + c2 + c3);
    }

    [Fact]
    public void CuotaCalculator_CalcularFechaCuota_AdjustsToLastDayOfMonthFor31st()
    {
        // Primera cuota el 31 de Enero
        var primeraCuota = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var fechaCuota1 = CuotaCalculator.CalcularFechaCuota(primeraCuota, 1);
        var fechaCuota2 = CuotaCalculator.CalcularFechaCuota(primeraCuota, 2); // Febrero (28 días en 2026)
        var fechaCuota3 = CuotaCalculator.CalcularFechaCuota(primeraCuota, 3); // Marzo (31 días)
        var fechaCuota4 = CuotaCalculator.CalcularFechaCuota(primeraCuota, 4); // Abril (30 días)

        Assert.Equal(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), fechaCuota1);
        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), fechaCuota2);
        Assert.Equal(new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), fechaCuota3);
        Assert.Equal(new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc), fechaCuota4);
    }

    [Fact]
    public async Task CreateAsync_CuotasWithCreditCard_CalculatesCuotaAndInitializesFields()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Santander",
            TipoTarjeta = TipoTarjeta.Credito
        });

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 120000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2026, 4, 10),
            Descripcion = "Smart TV",
            CategoryId = 2, // Hogar
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        };

        // Act
        var created = await manager.CreateAsync(input);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(120000m, created.MontoTotal);
        Assert.Equal(6, created.CantidadCuotas);
        Assert.Equal(0, created.CuotasPagadas);
        Assert.Equal(20000m, created.Monto); // 120.000 / 6 = 20.000 cuota base
        Assert.Equal(new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), created.FechaPrimeraCuota);
        Assert.Null(created.FechaPrimeraCuotaPendiente);
    }

    [Fact]
    public async Task CreateAsync_CuotasWithDebitCard_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjetaDebito = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Débito Galicia",
            TipoTarjeta = TipoTarjeta.Debito
        });

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 50000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 4, 10),
            Descripcion = "Ropa",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjetaDebito.Id
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.CreateAsync(input));
        Assert.Equal("Las compras en cuotas solo pueden realizarse con tarjetas de crédito.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenCuotasAlreadyPaid_ModifyingMontoTotalOrCantidadCuotas_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Mastercard BBVA",
            TipoTarjeta = TipoTarjeta.Credito
        });

        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 5, 1),
            Descripcion = "Zapatillas",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Simular que ya se pagó 1 cuota
        trans.CuotasPagadas = 1;
        await transactionStore.UpdateAsync(trans);

        // Act & Assert 1: Intentar cambiar monto total
        var updateConNuevoTotal = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 80000m, // Cambiado
            CantidadCuotas = 3,
            Fecha = trans.Fecha,
            Descripcion = "Zapatillas",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        };

        var ex1 = await Assert.ThrowsAsync<BusinessException>(() => manager.UpdateAsync(trans.Id, updateConNuevoTotal));
        Assert.Equal("No se puede modificar el monto total ni la cantidad de cuotas de una compra que ya tiene cuotas pagadas o adelantadas.", ex1.Message);

        // Act & Assert 2: Modificar solo descripción sí está permitido
        var updatePermitido = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 3,
            Fecha = trans.Fecha,
            Descripcion = "Zapatillas Nike Running", // Permitido
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        };

        var updated = await manager.UpdateAsync(trans.Id, updatePermitido);
        Assert.Equal("Zapatillas Nike Running", updated.Descripcion);
    }

    [Fact]
    public async Task GetPagedAsync_Cuotas_ShowsCuotaAmountInCorrespondingMonths()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Macro",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // Compra de $100.000 en 3 cuotas el 15/03/2026:
        // Marzo: $33.333,33
        // Abril: $33.333,33
        // Mayo:  $33.333,34
        await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 100000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 3, 15),
            Descripcion = "Notebook cuotas",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act 1: Consultar mes de Mayo 2026 (tercera y última cuota)
        var (itemsMayo, countMayo) = await manager.GetPagedAsync(
            desde: new DateTime(2026, 5, 1),
            hasta: new DateTime(2026, 5, 31),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 10);

        Assert.Equal(1, countMayo);
        Assert.Single(itemsMayo);
        Assert.Equal(new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc), itemsMayo[0].Fecha);
        Assert.Equal(33333.34m, itemsMayo[0].Monto); // Última cuota absorbe el centavo de redondeo

        // Act 2: Consultar mes de Abril 2026 (segunda cuota)
        var (itemsAbril, countAbril) = await manager.GetPagedAsync(
            desde: new DateTime(2026, 4, 1),
            hasta: new DateTime(2026, 4, 30),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 10);

        Assert.Equal(1, countAbril);
        Assert.Single(itemsAbril);
        Assert.Equal(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), itemsAbril[0].Fecha);
        Assert.Equal(33333.33m, itemsAbril[0].Monto);
    }

    [Fact]
    public void Validator_TransactionCreateInput_ValidatesCuotasCorrectly()
    {
        // Caso inválido: cuotas = 1 (debe ser mayor a 1)
        var inputCuota1 = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 10000m,
            CantidadCuotas = 1,
            Descripcion = "Inválido cuota 1",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = 1
        };

        var resCuota1 = _createValidator.TestValidate(inputCuota1);
        resCuota1.ShouldHaveValidationErrorFor(x => x.CantidadCuotas)
            .WithErrorMessage("La cantidad de cuotas debe ser mayor a 1.");

        // Caso inválido: cuotas sin tarjeta
        var inputSinTarjeta = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 10000m,
            CantidadCuotas = 3,
            Descripcion = "Inválido sin tarjeta",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };

        var resSinTarjeta = _createValidator.TestValidate(inputSinTarjeta);
        resSinTarjeta.ShouldHaveValidationErrorFor(x => x.MedioPago)
            .WithErrorMessage("Las compras en cuotas deben pagarse con Tarjeta.");
    }
}
