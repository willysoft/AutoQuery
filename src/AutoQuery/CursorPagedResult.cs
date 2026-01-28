namespace AutoQuery;

/// <summary>
/// Represents paginated results using cursor-based pagination.
/// </summary>
/// <typeparam name="TData">The type of data contained in the result set.</typeparam>
/// <param name="Datas">The data collection of the paginated result, represented as <see cref="IQueryable{T}"/>.</param>
/// <param name="NextPageToken">The page token for the next page. Null when no more pages exist.</param>
/// <param name="PreviousPageToken">The page token for the previous page. Null when on the first page or not supported.</param>
public record CursorPagedResult<TData>(
    IQueryable<TData> Datas,
    string? NextPageToken = null,
    string? PreviousPageToken = null) : IPagedResult<TData>;
