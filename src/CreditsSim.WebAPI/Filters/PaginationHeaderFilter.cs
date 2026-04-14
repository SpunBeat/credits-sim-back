using CreditsSim.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CreditsSim.WebAPI.Filters;

/// <summary>
/// Automatically moves pagination metadata from the response body to HTTP headers
/// when the action returns a <see cref="IPagedResult"/>.
/// The body is replaced with only the items array.
/// </summary>
public class PaginationHeaderFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: IPagedResult paged, StatusCode: 200 or null })
        {
            var headers = context.HttpContext.Response.Headers;
            headers.Append(PaginationHeaders.TotalCount, paged.TotalCount.ToString());
            headers.Append(PaginationHeaders.TotalPages, paged.TotalPages.ToString());
            headers.Append(PaginationHeaders.PageNumber, paged.PageNumber.ToString());
            headers.Append(PaginationHeaders.PageSize, paged.PageSize.ToString());

            // Replace body with items only — metadata lives in headers now
            context.Result = new OkObjectResult(paged.GetItems());
        }

        await next();
    }
}
