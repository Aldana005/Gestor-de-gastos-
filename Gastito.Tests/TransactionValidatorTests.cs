using FluentValidation.TestHelper;
using Gastito.Entities.Enums;
using Gastito.Presentation.Inputs.Transactions;
using Gastito.Presentation.Validators.Transactions;
using Xunit;

namespace Gastito.Tests;

public class TransactionValidatorTests
{
    private readonly TransactionCreateInputValidator _createValidator = new();
    private readonly TransactionUpdateInputValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_ValidGastoWithTarjeta_ShouldNotHaveAnyErrors()
    {
        var model = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto válido",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = 1
        };

        var result = _createValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_ValidIngreso_ShouldNotHaveAnyErrors()
    {
        var model = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Eventual,
            Monto = 50000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Ingreso válido",
            CategoryId = 4,
            MedioPago = null,
            TarjetaId = null
        };

        var result = _createValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_GastoWithoutMedioPago_ShouldHaveValidationError()
    {
        var model = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto sin medio",
            CategoryId = 1,
            MedioPago = null
        };

        var result = _createValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.MedioPago);
    }

    [Fact]
    public void CreateValidator_GastoTarjetaWithoutTarjetaId_ShouldHaveValidationError()
    {
        var model = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 1000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Gasto tarjeta sin id",
            CategoryId = 1,
            MedioPago = MedioPago.Tarjeta,
            TarjetaId = null
        };

        var result = _createValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TarjetaId);
    }

    [Fact]
    public void CreateValidator_IngresoWithMedioPago_ShouldHaveValidationError()
    {
        var model = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Eventual,
            Monto = 10000m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Ingreso con medio de pago inválido",
            CategoryId = 4,
            MedioPago = MedioPago.Efectivo
        };

        var result = _createValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.MedioPago);
    }

    [Fact]
    public void CreateValidator_ZeroOrNegativeMonto_ShouldHaveValidationError()
    {
        var model = new TransactionCreateInput
        {
            Tipo = TipoMovimiento.Ingreso,
            Frecuencia = Frecuencia.Eventual,
            Monto = 0m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Monto cero",
            CategoryId = 4
        };

        var result = _createValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Monto);
    }

    [Fact]
    public void UpdateValidator_ValidInput_ShouldNotHaveAnyErrors()
    {
        var model = new TransactionUpdateInput
        {
            Tipo = TipoMovimiento.Gasto,
            Frecuencia = Frecuencia.Eventual,
            Monto = 2500m,
            Fecha = DateTime.UtcNow,
            Descripcion = "Update válido",
            CategoryId = 1,
            MedioPago = MedioPago.Efectivo,
            TarjetaId = null
        };

        var result = _updateValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
