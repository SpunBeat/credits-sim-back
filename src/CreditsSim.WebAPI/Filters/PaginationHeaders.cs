namespace CreditsSim.WebAPI.Filters;

/// <summary>
/// Custom HTTP header names for cursor-based pagination metadata.
/// </summary>
public static class PaginationHeaders
{
    public const string TotalCount = "X-Total-Count";
    public const string PageSize = "X-Page-Size";
    public const string HasNextPage = "X-Has-Next-Page";
    public const string NextCursorCreatedAt = "X-Next-Cursor-CreatedAt";
    public const string NextCursorId = "X-Next-Cursor-Id";

    public static readonly string[] All =
        [TotalCount, PageSize, HasNextPage, NextCursorCreatedAt, NextCursorId];
}
