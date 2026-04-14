namespace CreditsSim.Application.DTOs;

/// <summary>
/// Non-generic interface for extracting pagination metadata in filters.
/// </summary>
public interface IPagedResult
{
    /// <summary>Total de registros en la base de datos.</summary>
    int TotalCount { get; }

    /// <summary>Número de página actual (1-based).</summary>
    int PageNumber { get; }

    /// <summary>Cantidad de elementos por página.</summary>
    int PageSize { get; }

    /// <summary>Total de páginas disponibles.</summary>
    int TotalPages { get; }

    /// <summary>Returns only the items list (untyped) for body serialization.</summary>
    object GetItems();
}

/// <summary>
/// Respuesta genérica paginada.
/// </summary>
/// <typeparam name="T">Tipo de los elementos en la lista.</typeparam>
public record PagedResponse<T> : IPagedResult
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

    /// <inheritdoc />
    public object GetItems() => Items;
}
