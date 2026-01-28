namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for cursor-based pagination.
/// </summary>
public interface IQueryCursorOptions : IQueryOptions
{
    /// <summary>
    /// Page token representing the cursor position for pagination.
    /// </summary>
    string? PageToken { get; set; }

    /// <summary>
    /// Number of items to return.
    /// </summary>
    int? PageSize { get; set; }
}
