using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.BusinessLogic.Utils;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Presentation.Controllers;
using Gastito.Presentation.Inputs.Cuotas;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Outputs.Cuotas;
using Gastito.Presentation.Validators.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class PendientesYAdelantarTests : IDisposable
{
    public PendientesYAdelantarTests()
    {
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
    public async Task AdelantarCuotas_ValidPayment_CreatesPagoCuotaAndUpdatesTransaction()
    {
        // Arrange: Simular pago en Marzo 2026
        DateTimeUtils.SetClock(() => new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));

        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Galicia",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // Compra de 6 cuotas de $10.000 (total $60.000) iniciada en Dic 2025:
        // Cuota 1: Dic 2025
        // Cuota 2: Ene 2026
        // Cuota 3: Feb 2026
        // Cuota 4: Mar 2026
        // Cuota 5: Abr 2026
        // Cuota 6: May 2026
        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2025, 12, 10),
            FechaPrimeraCuota = new DateTime(2025, 12, 10),
            Descripcion = "Aire acondicionado",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Ya tenía pagadas 3 cuotas (Dic, Ene, Feb)
        trans.CuotasPagadas = 3;
        await transactionStore.UpdateAsync(trans);

        // Act: En Marzo adelanta 2 cuotas (la 4 y la 5)
        var pago = await manager.AdelantarCuotasAsync(trans.Id, 2);

        // Assert PagoCuota
        Assert.NotNull(pago);
        Assert.Equal(2, pago.CantidadCuotas);
        Assert.Equal(20000m, pago.Monto); // 10.000 + 10.000
        Assert.Equal(4, pago.CuotaDesde);
        Assert.Equal(5, pago.CuotaHasta);
        Assert.Equal("cuotas 4 a 5", $"cuotas {pago.CuotaDesde} a {pago.CuotaHasta}");

        // Assert Transaction: cuotas pagadas pasa a 5
        var updated = await manager.GetByIdAsync(trans.Id);
        Assert.NotNull(updated);
        Assert.Equal(5, updated.CuotasPagadas);

        // Corrimiento de calendario: la cuota 6 pasa al mes siguiente al del pago (Abril 2026)
        Assert.NotNull(updated.FechaPrimeraCuotaPendiente);
        Assert.Equal(new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), updated.FechaPrimeraCuotaPendiente.Value);
    }

    [Fact]
    public async Task AdelantarCuotas_ConsecutiveCompression_ShiftsNextPendingToNextMonth()
    {
        // Arrange: Simular Marzo 2026
        DateTimeUtils.SetClock(() => new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));

        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Galicia",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // Compra de 6 cuotas iniciada en Enero 2026 (Ene: 1, Feb: 2, Mar: 3)
        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2026, 1, 5),
            Descripcion = "Celular",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // En Marzo paga 2 cuotas (1 y 2). Con compresión consecutiva, la cuota 3 pasa a Abril.
        await manager.AdelantarCuotasAsync(trans.Id, 2);

        // Assert: La próxima cuota (3) pasa al mes siguiente al del pago (Abril 2026)
        var updated = await manager.GetByIdAsync(trans.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.CuotasPagadas);
        Assert.Equal(new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), updated.FechaPrimeraCuotaPendiente);
    }

    [Fact]
    public async Task AdelantarCuotas_Adelanto4De6EnOctubre_Cuota5Noviembre_Cuota6Diciembre_SinCuotasFalsasEnEnero()
    {
        // Arrange: Simular Octubre 2026
        DateTimeUtils.SetClock(() => new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc));

        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Galicia",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // Compra 6 cuotas de $10.000 cargada el 26/10/2026
        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2026, 10, 26),
            FechaPrimeraCuota = new DateTime(2026, 10, 26),
            Descripcion = "Smart TV",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act: En Octubre se adelantan 4 cuotas
        var pago = await manager.AdelantarCuotasAsync(trans.Id, 4);

        // Assert: Cuotas pagadas = 4, próxima cuota pendiente = Noviembre 2026
        var updated = await manager.GetByIdAsync(trans.Id);
        Assert.NotNull(updated);
        Assert.Equal(4, updated.CuotasPagadas);
        Assert.Equal(new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc), updated.FechaPrimeraCuotaPendiente);

        // Verificar movimientos en Octubre 2026: deben figurar las 4 cuotas adelantadas
        var (movsOct, countOct) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2026, 10, 1),
            hasta: new DateTime(2026, 10, 31),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);
        Assert.Equal(4, countOct);
        Assert.Equal(40000m, movsOct.Sum(m => m.Monto));

        // Verificar movimientos en Noviembre 2026: debe figurar la Cuota 5
        var (movsNov, countNov) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2026, 11, 1),
            hasta: new DateTime(2026, 11, 30),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);
        Assert.Single(movsNov);
        Assert.Contains("Cuota 5/6", movsNov[0].Descripcion);
        Assert.Equal(10000m, movsNov[0].Monto);

        // Verificar movimientos en Diciembre 2026: debe figurar la Cuota 6
        var (movsDic, countDic) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2026, 12, 1),
            hasta: new DateTime(2026, 12, 31),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);
        Assert.Single(movsDic);
        Assert.Contains("Cuota 6/6", movsDic[0].Descripcion);
        Assert.Equal(10000m, movsDic[0].Monto);

        // Verificar movimientos en Enero 2027: NO debe figurar ninguna cuota (cero cuotas falsas)
        var (movsEne, countEne) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2027, 1, 1),
            hasta: new DateTime(2027, 1, 31),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);
        Assert.Empty(movsEne);

        // Verificar movimientos en Febrero 2027: NO debe figurar ninguna cuota
        var (movsFeb, countFeb) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2027, 2, 1),
            hasta: new DateTime(2027, 2, 28),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);
        Assert.Empty(movsFeb);
    }

    [Fact]
    public async Task Camino1_GastoCargadoEnOctubre_IniciaEnNoviembre_OctubreMontoCero_NoviembreCuota1()
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

        // Camino 1: compra cargada el 26/10/2026 pero 1ª cuota corre desde el 01/11/2026
        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2026, 10, 26),
            FechaPrimeraCuota = new DateTime(2026, 11, 1),
            Descripcion = "Lavarropas",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act & Assert 1: En Octubre 2026 figura el registro informativo de la compra con Monto = $0
        var (movsOct, countOct) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2026, 10, 1),
            hasta: new DateTime(2026, 10, 31),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);

        Assert.Single(movsOct);
        Assert.Equal(0m, movsOct[0].Monto);
        Assert.Contains("6 cuotas", movsOct[0].Descripcion);

        // Act & Assert 2: En Noviembre 2026 figura la Cuota 1/6 con Monto = $10.000
        var (movsNov, countNov) = await transactionStore.GetPagedAsync(
            desde: new DateTime(2026, 11, 1),
            hasta: new DateTime(2026, 11, 30),
            tipo: null,
            categoriaIds: null,
            pagina: 1,
            tamanoPagina: 50);

        Assert.Single(movsNov);
        Assert.Equal(10000m, movsNov[0].Monto);
        Assert.Contains("Cuota 1/6", movsNov[0].Descripcion);
    }

    [Fact]
    public async Task AdelantarCuotas_WhenExceedsRemainingCuotas_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Galicia",
            TipoTarjeta = TipoTarjeta.Credito
        });

        var trans = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 30000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 1, 1),
            Descripcion = "Zapatillas",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act & Assert: Intentar adelantar 4 cuotas cuando solo hay 3
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.AdelantarCuotasAsync(trans.Id, 4));
        Assert.Contains("No se pueden adelantar más cuotas de las que faltan", ex.Message);
    }

    [Fact]
    public async Task GetPendientesAsync_SortsCorrectly_SinCuotasThenTerminaEnPeriodoThenCuotasPagadas()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Galicia",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // 1. Gasto eventual sin cuotas: $50.000 en Marzo
        var gastoSinCuotaMenor = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 50000m,
            Fecha = new DateTime(2026, 3, 10),
            Descripcion = "Supermercado Mayorista",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        });

        // 2. Gasto eventual sin cuotas: $80.000 en Marzo (mayor monto)
        var gastoSinCuotaMayor = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 80000m,
            Fecha = new DateTime(2026, 3, 12),
            Descripcion = "Reparación auto",
            CategoryId = 3,
            MedioPago = MedioPago.Transferencia
        });

        // 3. Gasto Fijo: $400.000 en Marzo (NO DEBE ENTRAR EN PENDIENTES)
        await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Fijo,
            Monto = 400000m,
            Fecha = new DateTime(2026, 3, 1),
            Descripcion = "Alquiler Fijo",
            CategoryId = 2,
            MedioPago = MedioPago.Transferencia
        });

        // 4. Compra en cuotas que TERMINA en Marzo (6/6 cae en Marzo):
        // Inicia en Octubre 2025 (Oct: 1, Nov: 2, Dic: 3, Ene: 4, Feb: 5, Mar: 6)
        var cuotaQueTermina = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2025, 10, 15),
            Descripcion = "Colchón (termina en marzo)",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });
        cuotaQueTermina.CuotasPagadas = 5;
        await transactionStore.UpdateAsync(cuotaQueTermina);

        // 5. Compra en cuotas que NO termina en Marzo (3/6 pagadas):
        // Inicia en Enero 2026 (Ene: 1, Feb: 2, Mar: 3, Abr: 4, May: 5, Jun: 6)
        var cuotaMasPagada = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 120000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2026, 1, 20),
            Descripcion = "TV (3/6 pagadas)",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });
        cuotaMasPagada.CuotasPagadas = 2; // Al consultar marzo cae la 3ra
        await transactionStore.UpdateAsync(cuotaMasPagada);

        // 6. Compra en cuotas que NO termina en Marzo (1/6 pagadas):
        // Inicia en Marzo 2026 (Marzo es la 1ra cuota)
        var cuotaMenosPagada = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 60000m,
            CantidadCuotas = 6,
            Fecha = new DateTime(2026, 3, 5),
            Descripcion = "Celular (1/6)",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act: Consultar Pendientes de Marzo 2026
        var pendientes = await manager.GetPendientesAsync(
            desde: new DateTime(2026, 3, 1),
            hasta: new DateTime(2026, 3, 31),
            categoriaIds: null,
            montoMin: null,
            montoMax: null);

        // Assert
        Assert.Equal(5, pendientes.Count); // 2 sin cuotas + 3 cuotas (el fijo fue excluido)

        // Orden requerido por Sección 3.7:
        // 1. Gastos eventuales sin cuotas, de mayor a menor monto:
        Assert.Equal(gastoSinCuotaMayor.Id, pendientes[0].Id); // $80.000
        Assert.Equal(gastoSinCuotaMenor.Id, pendientes[1].Id); // $50.000

        // 2. Compras en cuotas:
        // a. Primero las que terminan dentro del rango:
        Assert.Equal(cuotaQueTermina.Id, pendientes[2].Id);

        // b. Después el resto, por cuotas pagadas de mayor a menor:
        Assert.Equal(cuotaMasPagada.Id, pendientes[3].Id); // 2 pagadas antes de 0 pagadas
        Assert.Equal(cuotaMenosPagada.Id, pendientes[4].Id); // 0 pagadas
    }

    [Fact]
    public async Task PendientesController_GetPendientes_ReturnsOkWithOutputs()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var controller = new PendientesController(manager);

        await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 15000m,
            Fecha = new DateTime(2026, 3, 10),
            Descripcion = "Gasto eventual pendiente",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        });

        var filter = new PendientesFilterInput
        {
            Desde = new DateTime(2026, 3, 1),
            Hasta = new DateTime(2026, 3, 31)
        };

        // Act
        var result = await controller.GetPendientes(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsType<List<PendienteItemOutput>>(okResult.Value);
        Assert.Single(items);
        Assert.Equal("Gasto eventual pendiente", items[0].Descripcion);
        Assert.False(items[0].EsCuota);
    }

    [Fact]
    public async Task MovimientosController_AdelantarCuotasAndGetPagos_ReturnsOk()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var controller = new MovimientosController(
            manager,
            new TransactionCreateInputValidator(),
            new TransactionUpdateInputValidator());

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Visa Galicia",
            TipoTarjeta = TipoTarjeta.Credito
        });

        var created = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MontoTotal = 30000m,
            CantidadCuotas = 3,
            Fecha = new DateTime(2026, 1, 1),
            Descripcion = "Compra 3 cuotas",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        });

        // Act 1: Adelantar 1 cuota
        var resultAdelanto = await controller.AdelantarCuotas(created.Id, new AdelantarCuotasInput { Cantidad = 1 }, CancellationToken.None);
        var okAdelanto = Assert.IsType<OkObjectResult>(resultAdelanto);
        var pagoOutput = Assert.IsType<PagoCuotaOutput>(okAdelanto.Value);
        Assert.Equal(1, pagoOutput.CantidadCuotas);
        Assert.Equal(10000m, pagoOutput.Monto);

        // Act 2: Obtener historial de pagos
        var resultPagos = await controller.GetPagosCuota(created.Id, CancellationToken.None);
        var okPagos = Assert.IsType<OkObjectResult>(resultPagos);
        var listPagos = Assert.IsType<List<PagoCuotaOutput>>(okPagos.Value);
        Assert.Single(listPagos);
        Assert.Equal("cuotas 1 a 1", listPagos[0].RangoCuotas);
    }
}
