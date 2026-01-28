namespace AutoQuery;

/// <summary>
/// Represents a generic record type for paginated results.
/// This type is kept for backward compatibility. New code should use <see cref="OffsetPagedResult{TData}"/> or <see cref="CursorPagedResult{TData}"/>.
/// </summary>
/// <typeparam name="TData">The type of data contained in the result set.</typeparam>
/// <param name="Datas">The data collection of the paginated result, represented as <see cref="IQueryable{T}"/>.</param>
/// <param name="Page">The current page number (starting from 1) for offset-based pagination. Null when using cursor-based pagination.</param>
/// <param name="TotalPages">The total number of pages for offset-based pagination. Null when using cursor-based pagination.</param>
/// <param name="Count">The total number of data items in the result set for offset-based pagination. Null when using cursor-based pagination.</param>
/// <param name="NextPageToken">The page token for the next page (cursor-based pagination). Null when using offset-based pagination or when no more pages exist.</param>
/// <param name="PreviousPageToken">The page token for the previous page (cursor-based pagination). Null when using offset-based pagination or not supported.</param>
/// <remarks>
/// This type is maintained for backward compatibility. For new code:
/// <list type="bullet">
/// <item><description>Use <see cref="OffsetPagedResult{TData}"/> for offset-based pagination</description></item>
/// <item><description>Use <see cref="CursorPagedResult{TData}"/> for cursor-based pagination</description></item>
/// </list>
/// </remarks>
[Obsolete("Use OffsetPagedResult<TData> or CursorPagedResult<TData> instead for cleaner API responses.")]
public record PagedResult<TData>(
    IQueryable<TData> Datas, 
    int? Page, 
    int? TotalPages, 
    int? Count,
    string? NextPageToken = null,
    string? PreviousPageToken = null) : IPagedResult<TData>;
