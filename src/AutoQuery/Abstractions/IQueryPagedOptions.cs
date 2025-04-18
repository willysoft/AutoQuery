namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for paginated results.
/// </summary>
public interface IQueryPagedOptions : IQueryOptions
{
    /// <summary>
    /// Current page number.
    /// </summary>
    int? Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    int? PageSize { get; set; }
}
