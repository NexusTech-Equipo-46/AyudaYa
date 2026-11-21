using System.ComponentModel.DataAnnotations;
using AyudaYa.Negocio;

namespace AyudaYa.Web.Validaciones;

/// <summary>Usa en los formularios la misma regla de teléfono que los servicios.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TelefonoValidoAttribute : ValidationAttribute
{
    public TelefonoValidoAttribute()
        : base("Escribí un teléfono válido, con característica (entre 8 y 13 dígitos).")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // La obligatoriedad y los campos opcionales se resuelven con [Required].
        if (value is null || value is string texto && string.IsNullOrWhiteSpace(texto))
        {
            return ValidationResult.Success;
        }

        return value is string telefono && ReglasNegocio.TelefonoValido(telefono, out _)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}
