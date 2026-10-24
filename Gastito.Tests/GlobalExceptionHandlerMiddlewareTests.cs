using System.IO;
using System.Text.Json;
using Gastito.BusinessLogic.Exceptions;
using Gastito.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gastito.Tests;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _loggerMock = new();
    private readonly Mock<IHostEnvironment> _envMock = new();

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        // Arrange
        bool nextCalled = false;
        RequestDelegate next = (HttpContext context) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object, _envMock.Object);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenBusinessExceptionThrown_Returns400ProblemDetails()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => throw new BusinessException("Validación de regla falló");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object, _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Regla de negocio no cumplida", problem.Title);
        Assert.Equal("Validación de regla falló", problem.Detail);
        Assert.Equal("/api/v1/test", problem.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundExceptionThrown_Returns404ProblemDetails()
    {
        // Arrange
        RequestDelegate next = (HttpContext _) => throw new KeyNotFoundException("Item 42 no encontrado");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object, _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/test/42";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Recurso no encontrado", problem.Title);
        Assert.Equal("Item 42 no encontrado", problem.Detail);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionInProduction_Returns500WithSafeMessage()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        RequestDelegate next = (HttpContext _) => throw new InvalidOperationException("DB connection crashed with password secret");
        var middleware = new GlobalExceptionHandlerMiddleware(next, _loggerMock.Object, _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/movimientos";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal("Error interno del servidor", problem.Title);
        Assert.DoesNotContain("secret", problem.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Ha ocurrido un error inesperado en el servidor.", problem.Detail);
    }
}
