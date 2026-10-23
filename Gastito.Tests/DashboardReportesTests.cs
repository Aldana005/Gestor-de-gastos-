using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.BusinessLogic.Utils;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Presentation.Controllers;
using Gastito.Presentation.Outputs.Dashboard;
using Gastito.Presentation.Outputs.Reportes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class DashboardReportesTests : IDisposable
{
    public DashboardReportesTests()
    {
        // Fijamos la fecha a Octubre 2026 para pruebas deterministas
        DateTimeUtils.SetClock(() => new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    public void Dispose()
    {
        DateTimeUtils.ResetClock();
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    [InlineData(24)]
    public async Task DashboardManager_InvalidMeses_ThrowsBusinessException(int meses)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new DashboardManager(transactionStore, categoryStore);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.GetDashboardAsync(meses));
        Assert.Contains("meses", ex.Message);
    }

    [Fact]
    public async Task DashboardManager_CalculatesCurrentMonthMetricsCorrectly()
    {
        // Arrange: Octubre 2026
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var txManager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);
        var dashboardManager = new DashboardManager(transactionStore, categoryStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Gold",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // 1. Ingreso eventual en Octubre: $500.000
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Eventual,
            Monto = 500000m,
            Fecha = new DateTime(2026, 10, 5),
            Descripcion = "Venta freelance",
            CategoryId = 4 // Ingreso Principal
        });

        // 2. Ingreso fijo activo: $1.000.000 (desde 2026-01-01)
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Fijo,
            Monto = 1000000m,
            Fecha = new DateTime(2026, 1, 1),
            Descripcion = "Sueldo mensual",
            CategoryId = 4
        });

        // 3. Gasto eventual sin cuotas en Octubre: $150.000 en Supermercado (Cat 1)
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 150000m,
            Fecha = new DateTime(2026, 10, 10),
            Descripcion = "Supermercado Coto",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        });

        // 4. Gasto fijo en Octubre: $300.000 en Hogar (Cat 2)
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 300000m,
            Fecha = new DateTime(2026, 1, 1),
            Descripcion = "Alquiler mensual",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        });

        // 5. Compra en cuotas: $90.000 en 3 cuotas ($30.000/mes) iniciada en Septiembre 2026 en Hogar (Cat 2)
        // Cuota 1: Sep 2026, Cuota 2: Oct 2026 (cae este mes!), Cuota 3: Nov 2026
        var cuotaCompra = await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 90000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 9, 15),
            Descripcion = "Mueble en cuotas",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act
        var dashboard = await dashboardManager.GetDashboardAsync(meses: 3);

        // Assert
        // Total ingresos Octubre = 500.000 + 1.000.000 = 1.500.000
        Assert.Equal(1500000m, dashboard.TotalIngresos);

        // Total gastos Octubre = 150.000 (super) + 300.000 (alquiler fijo) + 30.000 (cuota 2) = 480.000
        Assert.Equal(480000m, dashboard.TotalGastos);

        // Monto pendiente Octubre: la cuota 2 vence este mes y no está pagada = 30.000
        Assert.Equal(30000m, dashboard.MontoPendiente);

        // Gastos por categoría:
        // Cat 1 (Supermercado): 150.000 -> 150.000 / 480.000 = 31.25%
        // Cat 2 (Hogar): 300.000 + 30.000 = 330.000 -> 330.000 / 480.000 = 68.75%
        Assert.Equal(2, dashboard.GastosPorCategoria.Count);
        Assert.Equal(2, dashboard.GastosPorCategoria[0].CategoriaId);
        Assert.Equal(330000m, dashboard.GastosPorCategoria[0].Monto);
        Assert.Equal(68.75m, dashboard.GastosPorCategoria[0].Porcentaje);

        Assert.Equal(1, dashboard.GastosPorCategoria[1].CategoriaId);
        Assert.Equal(150000m, dashboard.GastosPorCategoria[1].Monto);
        Assert.Equal(31.25m, dashboard.GastosPorCategoria[1].Porcentaje);

        // Ingresos vs Gastos: 3 meses (Agosto 2026, Septiembre 2026, Octubre 2026)
        Assert.Equal(3, dashboard.IngresosVsGastos.Count);
        Assert.Equal("2026-08", dashboard.IngresosVsGastos[0].Periodo);
        Assert.Equal("2026-09", dashboard.IngresosVsGastos[1].Periodo);
        Assert.Equal("2026-10", dashboard.IngresosVsGastos[2].Periodo);
        Assert.Equal(1500000m, dashboard.IngresosVsGastos[2].TotalIngresos);
        Assert.Equal(480000m, dashboard.IngresosVsGastos[2].TotalGastos);

        // Próximas deudas:
        Assert.Single(dashboard.ProximasDeudas);
        var deuda = dashboard.ProximasDeudas[0];
        Assert.Equal(cuotaCompra.Id, deuda.Id);
        Assert.Equal("Mueble en cuotas", deuda.Descripcion);
        Assert.Equal(0, deuda.CuotasPagadas);
        Assert.Equal(3, deuda.CantidadCuotas);
        Assert.Equal(30000m, deuda.ValorCuota);
        Assert.Equal("2026-09", deuda.MesProximaCuota); // la primera cuota pendiente era sep
    }

    [Fact]
    public async Task DashboardManager_CuotaAdelantada_CountsInPaymentMonthAndRemovesFromPendiente()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var txManager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);
        var dashboardManager = new DashboardManager(transactionStore, categoryStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Mastercard Black",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // Compra de $100.000 en 2 cuotas ($50.000 c/u) con primera cuota en Octubre
        var compra = await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 100000m,
            CantidadCuotas = 2,
            Fecha = new DateTime(2026, 10, 1),
            Descripcion = "Notebook",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Adelantar 1 cuota en Octubre (hoy)
        await txManager.AdelantarCuotasAsync(compra.Id, 1);

        // Act
        var dashboard = await dashboardManager.GetDashboardAsync(meses: 3);

        // Assert
        // Total gastos en Octubre: $50.000 (el adelanto pagado en Octubre)
        Assert.Equal(50000m, dashboard.TotalGastos);

        // Monto pendiente en Octubre: 0 (la cuota 1 ya fue pagada/adelantada y la cuota 2 se corrió al mes siguiente Noviembre)
        Assert.Equal(0m, dashboard.MontoPendiente);

        // Próximas deudas debe mostrar 1/2 pagadas
        Assert.Single(dashboard.ProximasDeudas);
        Assert.Equal(1, dashboard.ProximasDeudas[0].CuotasPagadas);
        Assert.Equal(2, dashboard.ProximasDeudas[0].CantidadCuotas);
        Assert.Equal("2026-11", dashboard.ProximasDeudas[0].MesProximaCuota); // calendario corrido a noviembre
    }

    [Fact]
    public async Task ReportesManager_InvalidDateRange_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var transactionStore = new TransactionStore(context);
        var reportesManager = new ReportesManager(transactionStore, categoryStore);

        // Act & Assert: hasta antes que desde
        var ex = await Assert.ThrowsAsync<BusinessException>(() => reportesManager.GetResumenAsync(
            desde: new DateTime(2026, 10, 31),
            hasta: new DateTime(2026, 10, 1),
            categoriaIds: null));

        Assert.Contains("anterior", ex.Message);
    }

    [Fact]
    public async Task ReportesManager_FiltersByDateRangeAndCategoriesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var txManager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);
        var reportesManager = new ReportesManager(transactionStore, categoryStore);

        // 1. Gasto en Categoría 1 (Supermercado) en Septiembre: $40.000
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 40000m,
            Fecha = new DateTime(2026, 9, 20),
            Descripcion = "Super Sep",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        });

        // 2. Gasto en Categoría 1 (Supermercado) en Octubre: $60.000
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 60000m,
            Fecha = new DateTime(2026, 10, 10),
            Descripcion = "Super Oct",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        });

        // 3. Gasto en Categoría 3 (Transporte) en Octubre: $15.000
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 15000m,
            Fecha = new DateTime(2026, 10, 12),
            Descripcion = "Carga Sube",
            CategoryId = 3,
            MedioPago = MedioPago.Transferencia
        });

        // 4. Ingreso en Categoría 4 en Octubre: $200.000
        await txManager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Eventual,
            Monto = 200000m,
            Fecha = new DateTime(2026, 10, 1),
            Descripcion = "Cobro trabajo",
            CategoryId = 4
        });

        // Act 1: Consultar solo Octubre con filtro de Categoría 1 (Supermercado)
        var resumenFiltrado = await reportesManager.GetResumenAsync(
            desde: new DateTime(2026, 10, 1),
            hasta: new DateTime(2026, 10, 31),
            categoriaIds: new List<int> { 1 });

        // Assert 1:
        // Ingresos: 0 (porque la cat 4 no está en [1])
        Assert.Equal(0m, resumenFiltrado.TotalIngresos);
        // Gastos: solo los 60.000 de Cat 1 en Octubre (el de Sep está fuera de fecha, el de Cat 3 está fuera de categoría)
        Assert.Equal(60000m, resumenFiltrado.TotalGastos);
        Assert.Single(resumenFiltrado.GastosPorCategoria);
        Assert.Equal(1, resumenFiltrado.GastosPorCategoria[0].CategoriaId);
        Assert.Equal(60000m, resumenFiltrado.GastosPorCategoria[0].Monto);
        Assert.Equal(100m, resumenFiltrado.GastosPorCategoria[0].Porcentaje);

        // Act 2: Consultar todo Octubre sin filtro de categoría
        var resumenGeneral = await reportesManager.GetResumenAsync(
            desde: new DateTime(2026, 10, 1),
            hasta: new DateTime(2026, 10, 31),
            categoriaIds: null);

        // Assert 2:
        Assert.Equal(200000m, resumenGeneral.TotalIngresos);
        Assert.Equal(75000m, resumenGeneral.TotalGastos); // 60.000 + 15.000
        Assert.Equal(2, resumenGeneral.GastosPorCategoria.Count);
    }

    [Fact]
    public async Task DashboardController_GetDashboard_ReturnsOkWithDashboardOutput()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var transactionStore = new TransactionStore(context);
        var dashboardManager = new DashboardManager(transactionStore, categoryStore);
        var controller = new DashboardController(dashboardManager);

        // Act
        var actionResult = await controller.GetDashboard(meses: 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var output = Assert.IsType<DashboardOutput>(okResult.Value);
        Assert.NotNull(output);
        Assert.Equal(3, output.IngresosVsGastos.Count);
    }

    [Fact]
    public async Task ReportesController_GetResumen_ReturnsOkWithReporteResumenOutput()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var transactionStore = new TransactionStore(context);
        var reportesManager = new ReportesManager(transactionStore, categoryStore);
        var controller = new ReportesController(reportesManager);

        // Act
        var actionResult = await controller.GetResumen(
            desde: new DateTime(2026, 10, 1),
            hasta: new DateTime(2026, 10, 31),
            categoriaIds: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var output = Assert.IsType<ReporteResumenOutput>(okResult.Value);
        Assert.NotNull(output);
    }
}
