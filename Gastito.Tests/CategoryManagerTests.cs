using Gastito.BusinessLogic.Exceptions;
using Gastito.BusinessLogic.Managers;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class CategoryManagerTests
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
    public async Task GetAllAsync_FiltersByType_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new CategoryStore(context);
        var manager = new CategoryManager(store);

        // Act
        var gastos = await manager.GetAllAsync(TipoMovimiento.Gasto);
        var ingresos = await manager.GetAllAsync(TipoMovimiento.Ingreso);

        // Assert - De las 4 del seed: 3 son gasto y 1 es ingreso
        Assert.Equal(3, gastos.Count);
        Assert.All(gastos, g => Assert.Equal(TipoMovimiento.Gasto, g.Tipo));

        Assert.Single(ingresos);
        Assert.Equal("Ingreso Principal", ingresos[0].Nombre);
    }

    [Fact]
    public async Task UpdateAsync_SystemCategory_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new CategoryStore(context);
        var manager = new CategoryManager(store);

        // Act & Assert (Categoría 1 = Supermercado, EsSistema = true)
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            manager.UpdateAsync(1, "Supermercado Nuevo"));

        Assert.Contains("iniciales del sistema no se pueden modificar", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_SystemCategory_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new CategoryStore(context);
        var manager = new CategoryManager(store);

        // Act & Assert (Categoría 2 = Hogar, EsSistema = true)
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            manager.DeleteAsync(2));

        Assert.Contains("iniciales del sistema no se pueden eliminar", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_CategoryWithTransactions_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new CategoryStore(context);
        var manager = new CategoryManager(store);

        // Creamos categoría propia
        var customCat = await manager.CreateAsync("Salidas", TipoMovimiento.Gasto);

        // Le asociamos una transacción
        context.Transactions.Add(new Transaction
        {
            Id = 100,
            Descripcion = "Cena",
            Monto = 15000m,
            CategoryId = customCat.Id,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Fecha = DateTime.UtcNow.Date
        });
        await context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            manager.DeleteAsync(customCat.Id));

        Assert.Contains("tiene movimientos asociados", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameType_ThrowsBusinessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new CategoryStore(context);
        var manager = new CategoryManager(store);

        await manager.CreateAsync("Educación", TipoMovimiento.Gasto);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            manager.CreateAsync("educación ", TipoMovimiento.Gasto));

        Assert.Contains("Ya existe una categoría con el nombre", ex.Message);
    }

    [Fact]
    public async Task CreateUpdateDelete_CustomCategory_Succeeds()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var store = new CategoryStore(context);
        var manager = new CategoryManager(store);

        // 1. Create
        var created = await manager.CreateAsync("Gimnasio", TipoMovimiento.Gasto);
        Assert.NotNull(created);
        Assert.False(created.EsSistema);

        // 2. Update
        var updated = await manager.UpdateAsync(created.Id, "Gimnasio y Deportes");
        Assert.Equal("Gimnasio y Deportes", updated.Nombre);

        // 3. Delete
        await manager.DeleteAsync(created.Id);
        var deleted = await manager.GetByIdAsync(created.Id);
        Assert.Null(deleted);
    }
}
