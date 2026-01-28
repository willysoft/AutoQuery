namespace AutoQuery;

/// <summary>
/// Represents paginated results using offset-based pagination.
/// </summary>
/// <typeparam name="TData">The type of data contained in the result set.</typeparam>
/// <param name="Datas">The data collection of the paginated result, represented as <see cref="IQueryable{T}"/>.</param>
/// <param name="Page">The current page number (starting from 1).</param>
/// <param name="TotalPages">The total number of pages.</param>
/// <param name="Count">The total number of data items in the result set.</param>
/// <param name="NextPageToken">Optional page token for switching to cursor-based pagination. Null when not available.</param>
public record OffsetPagedResult<TData>(
    IQueryable<TData> Datas,
    int Page,
    int TotalPages,
    int Count,
    string? NextPageToken = null) : IPagedResult<TData>;
