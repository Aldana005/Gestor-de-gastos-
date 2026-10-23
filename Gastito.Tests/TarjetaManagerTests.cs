using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class TarjetaManagerTests
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
    public async Task CreateAsync_DuplicateName_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new TarjetaStore(context);
        var manager = new TarjetaManager(store);

        await manager.CreateAsync("Visa Santander", TipoTarjeta.Credito);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            manager.CreateAsync(" visa santander ", TipoTarjeta.Debito));

        Assert.Contains("Ya existe una tarjeta con el nombre", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_CardWithTransactions_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new TarjetaStore(context);
        var manager = new TarjetaManager(store);

        var tarjeta = await manager.CreateAsync("Mastercard BBVA", TipoTarjeta.Credito);

        context.Transactions.Add(new Transaction
        {
            Id = 200,
            Descripcion = "Supermercado en cuotas",
            Monto = 50000m,
            CategoryId = 1,
            TarjetaId = tarjeta.Id,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Fecha = DateTime.UtcNow.Date
        });
        await context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            manager.DeleteAsync(tarjeta.Id));

        Assert.Contains("tiene movimientos asociados", ex.Message);
    }

    [Fact]
    public async Task CreateUpdateDelete_Tarjeta_Succeeds()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new TarjetaStore(context);
        var manager = new TarjetaManager(store);

        // 1. Create
        var created = await manager.CreateAsync("Naranja X", TipoTarjeta.Credito);
        Assert.NotNull(created);
        Assert.Equal("Naranja X", created.Nombre);
        Assert.Equal(TipoTarjeta.Credito, created.TipoTarjeta);

        // 2. Update
        var updated = await manager.UpdateAsync(created.Id, "Naranja X Débito", TipoTarjeta.Debito);
        Assert.Equal("Naranja X Débito", updated.Nombre);
        Assert.Equal(TipoTarjeta.Debito, updated.TipoTarjeta);

        // 3. Delete
        await manager.DeleteAsync(created.Id);
        var deleted = await manager.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }
}
