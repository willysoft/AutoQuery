namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for paginated results.
/// </summary>
public interface IQueryPagedOptions : IQueryOptions
{
    /// <summary>
    /// Current page number (for offset-based pagination).
    /// </summary>
    int? Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    int? PageSize { get; set; }

    /// <summary>
    /// Page token for cursor-based pagination. When provided, this takes precedence over Page.
    /// </summary>
    string? PageToken { get; set; }
}
