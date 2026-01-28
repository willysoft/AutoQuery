namespace AutoQuery;

/// <summary>
/// Represents a cursor-based paginated result.
/// </summary>
/// <typeparam name="TData">The type of data contained in the result set.</typeparam>
/// <param name="Datas">The data collection of the paginated result, represented as <see cref="IQueryable{T}"/>.</param>
/// <param name="NextPageToken">The page token for the next page, or null if there are no more results.</param>
/// <param name="Count">The number of items in the current result set.</param>
public record CursorPagedResult<TData>(IQueryable<TData> Datas, string? NextPageToken, int Count);
