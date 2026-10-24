using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gastito.Presentation.Inputs.Categories;
using Gastito.Presentation.Outputs.Categories;
using Gastito.Presentation.Outputs.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Gastito.Tests;

public class IntegrationTests : IClassFixture<GastitoWebApplicationFactory>
{
    private readonly GastitoWebApplicationFactory _factory;

    public IntegrationTests(GastitoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategorias_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/categorias");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            usuario = "admin",
            contrasena = "admin"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<GastitoWebApplicationFactory.LoginTestResponse>();
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content.Token));
        Assert.True(content.ExpiraEnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            usuario = "admin",
            contrasena = "wrongpassword"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCategorias_WithAuthToken_ReturnsSeedCategories()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/categorias");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryOutput>>();
        Assert.NotNull(categories);
        Assert.True(categories.Count >= 4);
        Assert.Contains(categories, c => c.Nombre == "Supermercado");
        Assert.Contains(categories, c => c.Nombre == "Ingreso Principal");
    }

    [Fact]
    public async Task GetDashboard_WithAuthToken_ReturnsOk()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/dashboard?meses=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardOutput>();
        Assert.NotNull(dashboard);
        Assert.NotNull(dashboard.GastosPorCategoria);
        Assert.NotNull(dashboard.IngresosVsGastos);
        Assert.NotNull(dashboard.ProximasDeudas);
    }

    [Fact]
    public async Task DeleteSeedCategory_ReturnsBadRequest_WithProblemDetails()
    {
        // Arrange (Seed category Id 1: Supermercado is system category and cannot be deleted)
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync("/api/v1/categorias/1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Regla de negocio no cumplida", problem.Title);
        Assert.Contains("del sistema", problem.Detail);
    }

    [Fact]
    public async Task CreateCategory_WithEmptyName_ReturnsBadRequest_WithValidationProblemDetails()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/categorias", new
        {
            nombre = "",
            tipo = 1
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validaci", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetNonExistingCategory_ReturnsNotFound_WithProblemDetails()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/categorias/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }
}
