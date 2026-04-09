namespace CreditsSim.Application.DTOs;

public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
