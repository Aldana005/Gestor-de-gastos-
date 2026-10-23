namespace Gastito.Presentation.Outputs.Auth;

public class LoginOutput
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEnUtc { get; set; }
}
