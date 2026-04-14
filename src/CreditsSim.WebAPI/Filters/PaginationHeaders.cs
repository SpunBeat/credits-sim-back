namespace CreditsSim.WebAPI.Filters;

/// <summary>
/// Custom HTTP header names for pagination metadata.
/// </summary>
public static class PaginationHeaders
{
    public const string TotalCount = "X-Total-Count";
    public const string TotalPages = "X-Total-Pages";
    public const string PageNumber = "X-Page-Number";
    public const string PageSize = "X-Page-Size";

    public static readonly string[] All = [TotalCount, TotalPages, PageNumber, PageSize];
}
