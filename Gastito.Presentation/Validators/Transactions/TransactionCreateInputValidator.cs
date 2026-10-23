using FluentValidation;
using Gastito.Entities.Enums;
using Gastito.Presentation.Inputs.Transactions;

namespace Gastito.Presentation.Validators.Transactions;

public class TransactionCreateInputValidator : AbstractValidator<TransactionCreateInput>
{
    public TransactionCreateInputValidator()
    {
        When(x => !x.CantidadCuotas.HasValue || x.CantidadCuotas <= 1, () =>
        {
            RuleFor(x => x.Monto)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");
        });

        When(x => x.CantidadCuotas.HasValue && x.CantidadCuotas > 1, () =>
        {
            RuleFor(x => x.MontoTotal)
                .NotNull().WithMessage("El monto total es obligatorio para compras en cuotas.")
                .GreaterThan(0).WithMessage("El monto total debe ser mayor a 0.");

            RuleFor(x => x.Tipo)
                .Equal(TipoMovimiento.Gasto).WithMessage("Las compras en cuotas solo pueden ser de tipo Gasto.");

            RuleFor(x => x.Frecuencia)
                .Equal(Frecuencia.Eventual).WithMessage("Las compras en cuotas solo pueden tener frecuencia Eventual.");

            RuleFor(x => x.MedioPago)
                .Equal(MedioPago.Tarjeta).WithMessage("Las compras en cuotas deben pagarse con Tarjeta.");

            RuleFor(x => x.TarjetaId)
                .NotNull().WithMessage("Debe seleccionar una tarjeta para compras en cuotas.")
                .GreaterThan(0).WithMessage("Debe seleccionar una tarjeta válida.");
        });

        When(x => x.MontoTotal.HasValue, () =>
        {
            RuleFor(x => x.CantidadCuotas)
                .NotNull().WithMessage("Debe indicar la cantidad de cuotas.")
                .GreaterThan(1).WithMessage("La cantidad de cuotas debe ser mayor a 1.");
        });

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(250).WithMessage("La descripción no puede superar los 250 caracteres.");

        RuleFor(x => x.Notas)
            .MaximumLength(1000).WithMessage("Las notas no pueden superar los 1000 caracteres.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("La categoría es obligatoria.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de movimiento no es válido (1 = Ingreso, 2 = Gasto).");

        RuleFor(x => x.Frecuencia)
            .IsInEnum().WithMessage("La frecuencia no es válida (1 = Eventual, 2 = Fijo).");

        When(x => x.Tipo == TipoMovimiento.Gasto, () =>
        {
            RuleFor(x => x.MedioPago)
                .NotNull().WithMessage("El medio de pago es obligatorio para los gastos.")
                .IsInEnum().WithMessage("El medio de pago no es válido.");

            When(x => x.MedioPago == MedioPago.Tarjeta, () =>
            {
                RuleFor(x => x.TarjetaId)
                    .NotNull().WithMessage("Debe seleccionar una tarjeta cuando el medio de pago es Tarjeta.")
                    .GreaterThan(0).WithMessage("Debe seleccionar una tarjeta válida.");
            });

            When(x => x.MedioPago != MedioPago.Tarjeta, () =>
            {
                RuleFor(x => x.TarjetaId)
                    .Null().WithMessage("No se debe especificar tarjeta si el medio de pago no es Tarjeta.");
            });
        });

        When(x => x.Tipo == TipoMovimiento.Ingreso, () =>
        {
            RuleFor(x => x.MedioPago)
                .Null().WithMessage("Los ingresos no deben tener medio de pago.");

            RuleFor(x => x.TarjetaId)
                .Null().WithMessage("Los ingresos no deben tener tarjeta asociada.");
        });

        When(x => x.Frecuencia == Frecuencia.Fijo, () =>
        {
            RuleFor(x => x.FechaFin)
                .Must((input, fechaFin) =>
                {
                    if (!fechaFin.HasValue) return true;
                    var fechaInicio = input.FechaInicio ?? input.Fecha;
                    return fechaFin.Value.Date >= fechaInicio.Date;
                })
                .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
        });
    }
}
