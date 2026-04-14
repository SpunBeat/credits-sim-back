using CreditsSim.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CreditsSim.WebAPI.Filters;

/// <summary>
/// Automatically moves cursor pagination metadata from the response body to HTTP headers
/// when the action returns a <see cref="ICursorPagedResult"/>.
/// The body is replaced with only the items array.
/// </summary>
public class PaginationHeaderFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: ICursorPagedResult paged, StatusCode: 200 or null })
        {
            var headers = context.HttpContext.Response.Headers;

            headers.Append(PaginationHeaders.PageSize, paged.PageSize.ToString());
            headers.Append(PaginationHeaders.HasNextPage, paged.HasNextPage.ToString().ToLowerInvariant());

            if (paged.NextCursorCreatedAt.HasValue)
                headers.Append(PaginationHeaders.NextCursorCreatedAt,
                    paged.NextCursorCreatedAt.Value.ToString("O")); // ISO 8601

            if (paged.NextCursorId.HasValue)
                headers.Append(PaginationHeaders.NextCursorId,
                    paged.NextCursorId.Value.ToString());

            if (paged.TotalCount.HasValue)
                headers.Append(PaginationHeaders.TotalCount, paged.TotalCount.Value.ToString());

            context.Result = new OkObjectResult(paged.GetItems());
        }

        await next();
    }
}
