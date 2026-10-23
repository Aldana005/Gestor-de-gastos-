using FluentValidation;
using Gastito.Presentation.Inputs.Auth;

namespace Gastito.Presentation.Validators.Auth;

public class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("El campo usuario es obligatorio.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("El campo contraseña es obligatorio.");
    }
}
