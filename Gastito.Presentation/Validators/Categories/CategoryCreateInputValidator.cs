using FluentValidation;
using Gastito.Presentation.Inputs.Categories;

namespace Gastito.Presentation.Validators.Categories;

public class CategoryCreateInputValidator : AbstractValidator<CategoryCreateInput>
{
    public CategoryCreateInputValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la categoría es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de movimiento no es válido (1 = Ingreso, 2 = Gasto).");
    }
}
