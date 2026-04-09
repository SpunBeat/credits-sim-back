namespace CreditsSim.Application.DTOs;

/// <summary>
/// Respuesta genérica paginada.
/// </summary>
/// <typeparam name="T">Tipo de los elementos en la lista.</typeparam>
public record PagedResponse<T>
{
    /// <summary>Elementos de la página actual.</summary>
    public List<T> Items { get; init; } = [];

    /// <summary>Total de registros en la base de datos.</summary>
    public int TotalCount { get; init; }

    /// <summary>Número de página actual (1-based).</summary>
    public int PageNumber { get; init; }

    /// <summary>Cantidad de elementos por página.</summary>
    public int PageSize { get; init; }

    /// <summary>Total de páginas disponibles.</summary>
    public int TotalPages { get; init; }
}
