using FluentValidation;
using Gastito.BusinessLogic.Managers;
using Gastito.DataAccess;
using Gastito.DataAccess.Stores;
using Gastito.Entities.Enums;
using Gastito.Entities.Models;
using Gastito.Presentation.Controllers;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Outputs.Common;
using Gastito.Presentation.Outputs.Transactions;
using Gastito.Presentation.Validators.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gastito.Tests;

public class MovimientosControllerTests
{
    private readonly IValidator<TransactionCreateInput> _createValidator = new TransactionCreateInputValidator();
    private readonly IValidator<TransactionUpdateInput> _updateValidator = new TransactionUpdateInputValidator();

    private (MovimientosController Controller, AppDbContext Context) CreateControllerWithContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var categoryStore = new CategoryStore(context);
        var tarjetaStore = new TarjetaStore(context);
        var transactionStore = new TransactionStore(context);
        var manager = new TransactionManager(transactionStore, categoryStore, tarjetaStore);

        var controller = new MovimientosController(manager, _createValidator, _updateValidator);
        return (controller, context);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        // Arrange
        var (controller, context) = CreateControllerWithContext();
        context.Transactions.Add(new Transaction
        {
            Id = 1,
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1200m,
            Descripcion = "Almuerzo",
            CategoryId = 1, // Supermercado
            MedioPago = MedioPago.Efectivo,
            Fecha = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var filter = new TransactionFilterInput { Pagina = 1, TamanoPagina = 10 };

        // Act
        var result = await controller.GetAll(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<TransactionOutput>>(okResult.Value);
        Assert.Equal(1, paged.TotalRegistros);
        Assert.Single(paged.Items);
        Assert.Equal("Almuerzo", paged.Items[0].Descripcion);
        Assert.Equal("Supermercado", paged.Items[0].CategoryNombre);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404ProblemDetails()
    {
        // Arrange
        var (controller, _) = CreateControllerWithContext();

        // Act
        var result = await controller.GetById(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task Create_WhenInvalidModel_Returns400ValidationProblemDetails()
    {
        // Arrange
        var (controller, _) = CreateControllerWithContext();
        var invalidInput = new TransactionCreateInput
        {
            Monto = -10, // Inválido
            Descripcion = "" // Inválido
        };

        // Act
        var result = await controller.Create(invalidInput, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<HttpValidationProblemDetails>(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WhenBusinessRuleFails_Returns400ProblemDetails()
    {
        // Arrange
        var (controller, _) = CreateControllerWithContext();
        var input = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1500m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto con categoria de ingreso",
            CategoryId = 4, // Ingreso Principal (no coincide)
            MedioPago = MedioPago.Efectivo
        };

        // Act
        var result = await controller.Create(input, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Regla de negocio no cumplida", problem.Title);
        Assert.Contains("no coincide", problem.Detail);
    }

    [Fact]
    public async Task Create_WhenValid_Returns201Created()
    {
        // Arrange
        var (controller, _) = CreateControllerWithContext();
        var input = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 2500m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto ok",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };

        // Act
        var result = await controller.Create(input, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        var output = Assert.IsType<TransactionOutput>(createdResult.Value);
        Assert.True(output.Id > 0);
        Assert.Equal(2500m, output.Monto);
    }

    [Fact]
    public async Task Update_WhenNotFound_Returns404()
    {
        // Arrange
        var (controller, _) = CreateControllerWithContext();
        var input = new TransactionUpdateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto inexistente",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };

        // Act
        var result = await controller.Update(999, input, ct: CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task Update_WhenValid_Returns200Ok()
    {
        // Arrange
        var (controller, context) = CreateControllerWithContext();
        var trans = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Original",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };
        context.Transactions.Add(trans);
        await context.SaveChangesAsync();

        var updateInput = new TransactionUpdateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1500m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Modificado",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };

        // Act
        var result = await controller.Update(trans.Id, updateInput, ct: CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var output = Assert.IsType<TransactionOutput>(okResult.Value);
        Assert.Equal(1500m, output.Monto);
        Assert.Equal("Modificado", output.Descripcion);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        // Arrange
        var (controller, _) = CreateControllerWithContext();

        // Act
        var result = await controller.Delete(888, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task Delete_WhenFound_Returns204NoContent()
    {
        // Arrange
        var (controller, context) = CreateControllerWithContext();
        var trans = new Transaction
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "A eliminar",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo
        };
        context.Transactions.Add(trans);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.Delete(trans.Id, CancellationToken.None);

        // Assert
        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);

        // Verificar que quedó soft-deleted
        var directCheck = await context.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == trans.Id);
        Assert.NotNull(directCheck);
        Assert.True(directCheck.IsDeleted);
    }
}
