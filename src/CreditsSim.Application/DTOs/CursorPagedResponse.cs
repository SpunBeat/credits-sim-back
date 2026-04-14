namespace CreditsSim.Application.DTOs;

/// <summary>
/// Non-generic interface for extracting cursor pagination metadata in filters.
/// </summary>
public interface ICursorPagedResult
{
    /// <summary>Cantidad de elementos por página.</summary>
    int PageSize { get; }

    /// <summary>True if there are more items after this page.</summary>
    bool HasNextPage { get; }

    /// <summary>CreatedAt of the last item (cursor for next request). Null if empty.</summary>
    DateTime? NextCursorCreatedAt { get; }

    /// <summary>Id of the last item (tiebreaker cursor). Null if empty.</summary>
    Guid? NextCursorId { get; }

    /// <summary>Total records matching the filter (optional, may be null to skip the count).</summary>
    int? TotalCount { get; }

    /// <summary>Returns only the items list (untyped) for body serialization.</summary>
    object GetItems();
}

/// <summary>
/// Respuesta paginada basada en cursor (keyset pagination).
/// </summary>
/// <typeparam name="T">Tipo de los elementos en la lista.</typeparam>
public record CursorPagedResponse<T> : ICursorPagedResult
{
    /// <summary>Elementos de la página actual.</summary>
    public List<T> Items { get; init; } = [];

    /// <summary>Cantidad de elementos por página.</summary>
    public int PageSize { get; init; }

    /// <summary>True if there are more items after this page.</summary>
    public bool HasNextPage { get; init; }

    /// <summary>CreatedAt of the last item (cursor for next request). Null if empty.</summary>
    public DateTime? NextCursorCreatedAt { get; init; }

    /// <summary>Id of the last item (tiebreaker cursor). Null if empty.</summary>
    public Guid? NextCursorId { get; init; }

    /// <summary>Total records matching the filter (optional).</summary>
    public int? TotalCount { get; init; }

    /// <inheritdoc />
    public object GetItems() => Items;
}
