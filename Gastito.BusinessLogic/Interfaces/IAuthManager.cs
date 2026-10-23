namespace Gastito.BusinessLogic.Interfaces;

public record LoginResult(string Token, DateTime ExpiraEnUtc);

public interface IAuthManager
{
    LoginResult? Authenticate(string usuario, string contrasena);
}
