using Gastito.DataAccess;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class ModelPersistenceTests
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
    public void DbContext_SeedsInitialCategories_Successfully()
    {
        // Arrange & Act
        using var context = CreateInMemoryDbContext();
        var categories = context.Categories.OrderBy(c => c.Id).ToList();

        // Assert
        Assert.Equal(4, categories.Count);

        var supermercado = categories.FirstOrDefault(c => c.Id == 1);
        Assert.NotNull(supermercado);
        Assert.Equal("Supermercado", supermercado.Nombre);
        Assert.Equal(TipoMovimiento.Gasto, supermercado.Tipo);
        Assert.True(supermercado.EsSistema);

        var hogar = categories.FirstOrDefault(c => c.Id == 2);
        Assert.NotNull(hogar);
        Assert.Equal("Hogar", hogar.Nombre);
        Assert.Equal(TipoMovimiento.Gasto, hogar.Tipo);
        Assert.True(hogar.EsSistema);

        var transporte = categories.FirstOrDefault(c => c.Id == 3);
        Assert.NotNull(transporte);
        Assert.Equal("Transporte", transporte.Nombre);
        Assert.Equal(TipoMovimiento.Gasto, transporte.Tipo);
        Assert.True(transporte.EsSistema);

        var ingresoPrincipal = categories.FirstOrDefault(c => c.Id == 4);
        Assert.NotNull(ingresoPrincipal);
        Assert.Equal("Ingreso Principal", ingresoPrincipal.Nombre);
        Assert.Equal(TipoMovimiento.Ingreso, ingresoPrincipal.Tipo);
        Assert.True(ingresoPrincipal.EsSistema);
    }

    [Fact]
    public void GlobalQueryFilter_FiltersOutSoftDeletedTransactions()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var activeTransaction = new Transaction
        {
            Id = 10,
            Descripcion = "Compra activa",
            Monto = 1500.50m,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Moneda = Moneda.ARS,
            Fecha = DateTime.UtcNow.Date,
            CategoryId = 1,
            IsDeleted = false
        };

        var deletedTransaction = new Transaction
        {
            Id = 11,
            Descripcion = "Gasto borrado",
            Monto = 500.00m,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Moneda = Moneda.ARS,
            Fecha = DateTime.UtcNow.Date,
            CategoryId = 1,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        };

        context.Transactions.AddRange(activeTransaction, deletedTransaction);
        context.SaveChanges();

        // Act - Standard query with global filter
        var visibleTransactions = context.Transactions.ToList();
        // Act - Query ignoring filters (for audit / recovery)
        var allTransactions = context.Transactions.IgnoreQueryFilters().ToList();

        // Assert
        Assert.Single(visibleTransactions);
        Assert.Equal("Compra activa", visibleTransactions[0].Descripcion);

        Assert.Equal(2, allTransactions.Count);
    }

    [Fact]
    public void GlobalQueryFilter_FiltersOutQuotasAndHistory_WhenTransactionIsSoftDeleted()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var deletedTransaction = new Transaction
        {
            Id = 20,
            Descripcion = "Gasto con cuotas eliminado",
            Monto = 30000m,
            MontoTotal = 30000m,
            CantidadCuotas = 3,
            CuotasPagadas = 1,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Moneda = Moneda.ARS,
            Fecha = DateTime.UtcNow.Date,
            CategoryId = 1,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow
        };

        var pago = new PagoCuota
        {
            Id = 1,
            TransactionId = 20,
            Transaction = deletedTransaction,
            CantidadCuotas = 1,
            Monto = 10000m,
            CuotaDesde = 1,
            CuotaHasta = 1,
            FechaPagoUtc = DateTime.UtcNow
        };

        context.Transactions.Add(deletedTransaction);
        context.PagosCuota.Add(pago);
        context.SaveChanges();

        // Act
        var visiblePagos = context.PagosCuota.ToList();
        var allPagos = context.PagosCuota.IgnoreQueryFilters().ToList();

        // Assert
        Assert.Empty(visiblePagos);
        Assert.Single(allPagos);
    }

    [Fact]
    public void Tarjeta_CanBeCreatedAndAssociatedWithTransaction()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var tarjeta = new Tarjeta
        {
            Id = 1,
            Nombre = "Visa Santander",
            TipoTarjeta = TipoTarjeta.Credito
        };
        context.Tarjetas.Add(tarjeta);

        var transaction = new Transaction
        {
            Id = 30,
            Descripcion = "Compra zapatillas",
            Monto = 120000m,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = 1,
            CategoryId = 1,
            Fecha = DateTime.UtcNow.Date
        };
        context.Transactions.Add(transaction);
        context.SaveChanges();

        // Act
        var loaded = context.Transactions.Include(t => t.Tarjeta).FirstOrDefault(t => t.Id == 30);

        // Assert
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Tarjeta);
        Assert.Equal("Visa Santander", loaded.Tarjeta.Nombre);
        Assert.Equal(TipoTarjeta.Credito, loaded.Tarjeta.TipoTarjeta);
    }
}
