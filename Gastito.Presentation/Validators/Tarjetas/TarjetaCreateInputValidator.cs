using FluentValidation;
using Gastito.Presentation.Inputs.Tarjetas;

namespace Gastito.Presentation.Validators.Tarjetas;

public class TarjetaCreateInputValidator : AbstractValidator<TarjetaCreateInput>
{
    public TarjetaCreateInputValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la tarjeta es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.TipoTarjeta)
            .IsInEnum().WithMessage("El tipo de tarjeta no es válido (1 = Crédito, 2 = Débito).");
    }
}
