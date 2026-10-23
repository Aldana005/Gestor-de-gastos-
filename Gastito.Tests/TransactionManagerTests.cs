using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class TransactionManagerTests
{
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
    public async Task CreateAsync_ValidGastoWithTarjeta_Succeeds()
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

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 15000m,
            Fecha = new DateTime(2026, 3, 15, 14, 30, 0),
            Descripcion = "Supermercado Coto",
            Notas = "Compra semanal",
            CategoryId = 1, // Supermercado (Gasto del seed)
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        };

        // Act
        var created = await manager.CreateAsync(input);

        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(15000m, created.Monto);
        Assert.Equal(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), created.Fecha);
        Assert.Equal(DateTimeKind.Utc, created.Fecha.Kind);
        Assert.Equal("Supermercado Coto", created.Descripcion);
        Assert.Equal(1, created.CategoryId);
        Assert.Equal(MedioPago.Tarjeta, created.MedioPago);
        Assert.Equal(tarjeta.Id, created.TarjetaId);
        Assert.False(created.IsDeleted);
        Assert.Null(created.DeletedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ValidIngreso_ClearsMedioPagoAndTarjeta()
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
            TipoTarjeta = TipoTarjeta.Debito
        });

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Eventual,
            Monto = 350000m,
            Fecha = new DateTime(2026, 4, 1),
            Descripcion = "Cobro honorarios",
            CategoryId = 4, // Ingreso Principal (Ingreso del seed)
            MedioPago = MedioPago.Transferencia, // Debe limpiarse en ingresos
            TarjetaId = tarjeta.Id
        };

        // Act
        var created = await manager.CreateAsync(input);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(TipoMovimiento.Ingreso, created.Tipo);
        Assert.Null(created.MedioPago);
        Assert.Null(created.TarjetaId);
    }

    [Fact]
    public async Task CreateAsync_CategoryTypeMismatch_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 5000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Error categoria",
            CategoryId = 4, // Categoría 4 es "Ingreso Principal" (Ingreso)
            MedioPago = MedioPago.Efectivo
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.CreateAsync(input));
        Assert.Contains("no coincide con el movimiento", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_GastoWithoutMedioPago_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 5000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto sin medio",
            CategoryId = 1, // Supermercado
            MedioPago = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.CreateAsync(input));
        Assert.Contains("El medio de pago es obligatorio", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_GastoTarjetaWithoutTarjetaId_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var input = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 5000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto tarjeta sin id",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.CreateAsync(input));
        Assert.Contains("Debe seleccionar una tarjeta", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_PerformsSoftDelete_ItemHiddenFromQueries()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var created = await manager.CreateAsync(new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1200m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto a borrar",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        });

        // Act - Borrado suave
        await manager.DeleteAsync(created.Id);

        // Assert - GetByIdAsync no lo encuentra
        var queried = await manager.GetByIdAsync(created.Id);
        Assert.Null(queried);

        // Assert - En la BD directa con IgnoreQueryFilters sigue existiendo marcado como borrado
        var inDb = await context.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == created.Id);
        Assert.NotNull(inDb);
        Assert.True(inDb.IsDeleted);
        Assert.NotNull(inDb.DeletedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WhenInstallmentsPaid_CannotChangeAmountOrInstallments()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var tarjeta = await tarjetaStore.CreateAsync(new Tarjeta
        {
            Nombre = "Mastercard",
            TipoTarjeta = TipoTarjeta.Credito
        });

        // Insertamos directamente con cuotas pagadas
        var trans = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 30000m,
            MontoTotal = 30000m,
            CantidadCuotas = 3,
            CuotasPagadas = 1, // Ya tiene cuotas pagadas
            Fecha = DateTime.UtcNow,
            Descripcion = "Electrodoméstico",
            CategoryId = 2, // Hogar
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        };
        context.Transactions.Add(trans);
        await context.SaveChangesAsync();

        var updateAttempt = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 40000m, // Cambio de monto prohibido
            MontoTotal = 40000m,
            CantidadCuotas = 3,
            Fecha = DateTime.UtcNow,
            Descripcion = "Electrodoméstico nuevo",
            CategoryId = 2,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = tarjeta.Id
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => manager.UpdateAsync(trans.Id, updateAttempt));
        Assert.Contains("No se puede modificar el monto total ni la cantidad de cuotas", ex.Message);
    }

    [Fact]
    public async Task GetPagedAsync_FilteringAndPagination_WorksAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        // Agregamos varios movimientos
        for (int i = 1; i <= 15; i++)
        {
            await manager.CreateAsync(new Transaction
            {
                Tipo = i % 2 == 0 ? TipoMovimiento.Gasto : TipoMovimiento.Ingreso,
                Frecuencia = Frecuencia.Eventual,
                Monto = 1000m * i,
                Fecha = new DateTime(2026, 3, i),
                Descripcion = $"Movimiento {i}",
                CategoryId = i % 2 == 0 ? 1 : 4,
                MedioPago = i % 2 == 0 ? MedioPago.Efectivo : null
            });
        }

        // Act 1: Paginación básica (página 1 de 10)
        var (p1Items, p1Total) = await manager.GetPagedAsync(null, null, null, null, 1, 10);
        Assert.Equal(15, p1Total);
        Assert.Equal(10, p1Items.Count);

        // Act 2: Página 2 (deberían quedar 5)
        var (p2Items, p2Total) = await manager.GetPagedAsync(null, null, null, null, 2, 10);
        Assert.Equal(15, p2Total);
        Assert.Equal(5, p2Items.Count);

        // Act 3: Filtro por tipo Gasto
        var (gastoItems, gastoTotal) = await manager.GetPagedAsync(null, null, TipoMovimiento.Gasto, null, 1, 20);
        Assert.Equal(7, gastoTotal); // 2, 4, 6, 8, 10, 12, 14
        Assert.Equal(7, gastoItems.Count);
        Assert.All(gastoItems, g => Assert.Equal(TipoMovimiento.Gasto, g.Tipo));

        // Act 4: Filtro por rango de fechas (del 5 al 10 de marzo)
        var (dateItems, dateTotal) = await manager.GetPagedAsync(
            new DateTime(2026, 3, 5),
            new DateTime(2026, 3, 10),
            null,
            null,
            1,
            20);
        Assert.Equal(6, dateTotal); // 5, 6, 7, 8, 9, 10
        Assert.Equal(6, dateItems.Count);

        // Act 5: Filtro por categoría (Supermercado Id = 1)
        var (catItems, catTotal) = await manager.GetPagedAsync(
            null,
            null,
            null,
            new List<int> { 1 },
            1,
            20);
        Assert.Equal(7, catTotal);
        Assert.All(catItems, c => Assert.Equal(1, c.CategoryId));
    }
}
