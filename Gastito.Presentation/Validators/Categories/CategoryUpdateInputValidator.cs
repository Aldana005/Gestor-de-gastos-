using FluentValidation;
using Gastito.Presentation.Inputs.Categories;

namespace Gastito.Presentation.Validators.Categories;

public class CategoryUpdateInputValidator : AbstractValidator<CategoryUpdateInput>
{
    public CategoryUpdateInputValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la categoría es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
    }
}
