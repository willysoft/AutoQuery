namespace AutoQuery;

/// <summary>
/// Represents a generic record type for paginated results.
/// </summary>
/// <typeparam name="TData">The type of data contained in the result set.</typeparam>
/// <param name="Datas">The data collection of the paginated result, represented as <see cref="IQueryable{T}"/>.</param>
/// <param name="Page">The current page number (starting from 1).</param>
/// <param name="TotalPages">The total number of pages.</param>
/// <param name="Count">The total number of data items in the result set.</param>
public record PagedResult<TData>(IQueryable<TData> Datas, int Page, int TotalPages, int Count);
