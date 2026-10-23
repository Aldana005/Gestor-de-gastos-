using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Gastito.BusinessLogic.Managers;
using Gastito.Presentation.Controllers;
using Gastito.Presentation.Inputs.Auth;
using Gastito.Presentation.Outputs.Auth;
using Gastito.Presentation.Validators.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Gastito.Tests;

public class AuthTests
{
    private IConfiguration CreateTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Auth:FixedUser:Username", "admin" },
            { "Auth:FixedUser:Password", "admin" },
            { "Auth:Jwt:Key", "GastitoSecretKey_2026_AdvancedFinancialManager_SuperSecureKey_Min32Chars" },
            { "Auth:Jwt:Issuer", "GastitoApi" },
            { "Auth:Jwt:Audience", "GastitoApp" },
            { "Auth:Jwt:DurationMinutes", "60" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void Authenticate_WithValidCredentials_ReturnsValidJwtToken()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var authManager = new AuthManager(configuration);

        // Act
        var result = authManager.Authenticate("admin", "admin");

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiraEnUtc > DateTime.UtcNow);

        // Validar contenido del token JWT decodificado
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Token);

        Assert.Equal("GastitoApi", jwtToken.Issuer);
        Assert.Contains("GastitoApp", jwtToken.Audiences);

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name");
        Assert.NotNull(nameClaim);
        Assert.Equal("admin", nameClaim.Value);
    }

    [Fact]
    public void Authenticate_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var authManager = new AuthManager(configuration);

        // Act
        var result = authManager.Authenticate("admin", "wrong_password");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Authenticate_WithInvalidUsername_ReturnsNull()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var authManager = new AuthManager(configuration);

        // Act
        var result = authManager.Authenticate("non_admin", "admin");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthController_Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var authManager = new AuthManager(configuration);
        var validator = new LoginInputValidator();
        var controller = new AuthController(authManager, validator);

        var input = new LoginInput
        {
            Usuario = "admin",
            Contrasena = "admin"
        };

        // Act
        var actionResult = await controller.Login(input);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var output = Assert.IsType<LoginOutput>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(output.Token));
        Assert.True(output.ExpiraEnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task AuthController_Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var authManager = new AuthManager(configuration);
        var validator = new LoginInputValidator();
        var controller = new AuthController(authManager, validator);

        var input = new LoginInput
        {
            Usuario = "admin",
            Contrasena = "incorrecta"
        };

        // Act
        var actionResult = await controller.Login(input);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
    }

    [Fact]
    public async Task AuthController_Login_WithEmptyFields_ReturnsBadRequest()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var authManager = new AuthManager(configuration);
        var validator = new LoginInputValidator();
        var controller = new AuthController(authManager, validator);

        var input = new LoginInput
        {
            Usuario = "",
            Contrasena = ""
        };

        // Act
        var actionResult = await controller.Login(input);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problemDetails = Assert.IsType<HttpValidationProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.True(problemDetails.Errors.ContainsKey(nameof(LoginInput.Usuario)));
        Assert.True(problemDetails.Errors.ContainsKey(nameof(LoginInput.Contrasena)));
    }
}
