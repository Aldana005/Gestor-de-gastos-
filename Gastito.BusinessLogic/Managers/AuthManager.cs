using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gastito.BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Gastito.BusinessLogic.Managers;

public class AuthManager : IAuthManager
{
    private readonly IConfiguration _configuration;

    public AuthManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResult? Authenticate(string usuario, string contrasena)
    {
        var fixedUsername = _configuration["Auth:FixedUser:Username"] ?? "admin";
        var fixedPassword = _configuration["Auth:FixedUser:Password"] ?? "admin";

        // Comparación con el usuario fijo definido en configuración (Sección 3.1)
        if (!string.Equals(usuario?.Trim(), fixedUsername, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(contrasena, fixedPassword, StringComparison.Ordinal))
        {
            return null;
        }

        var jwtKey = _configuration["Auth:Jwt:Key"] 
            ?? "GastitoSecretKey_2026_AdvancedFinancialManager_SuperSecureKey_Min32Chars";
        var jwtIssuer = _configuration["Auth:Jwt:Issuer"] ?? "GastitoApi";
        var jwtAudience = _configuration["Auth:Jwt:Audience"] ?? "GastitoApp";
        var durationMinutes = int.TryParse(_configuration["Auth:Jwt:DurationMinutes"], out var mins) ? mins : 60;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);
        var expiraEnUtc = DateTime.UtcNow.AddMinutes(durationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, fixedUsername),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = expiraEnUtc,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResult(tokenString, expiraEnUtc);
    }
}
