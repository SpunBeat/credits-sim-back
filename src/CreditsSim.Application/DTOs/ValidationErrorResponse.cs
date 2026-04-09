namespace CreditsSim.Application.DTOs;

/// <summary>
/// Respuesta estándar de error de validación (RFC 7807 + extensión errors).
/// </summary>
public record ValidationErrorResponse
{
    /// <summary>URI que identifica el tipo de problema.</summary>
    public string? Type { get; init; }

    /// <summary>Título corto del error.</summary>
    public string Title { get; init; } = "Validation Error";

    /// <summary>Código de estado HTTP.</summary>
    public int Status { get; init; } = 400;

    /// <summary>Descripción detallada del error.</summary>
    public string Detail { get; init; } = "One or more validation errors occurred.";

    /// <summary>Lista de errores de validación por campo.</summary>
    public List<ValidationFieldError> Errors { get; init; } = [];
}

/// <summary>
/// Error de validación asociado a un campo específico.
/// </summary>
public record ValidationFieldError
{
    /// <summary>Nombre de la propiedad que falló la validación.</summary>
    public string PropertyName { get; init; } = string.Empty;

    /// <summary>Mensaje descriptivo del error.</summary>
    public string ErrorMessage { get; init; } = string.Empty;
}
