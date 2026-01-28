namespace AutoQuery.Abstractions;

/// <summary>
/// Base interface for paginated query options.
/// </summary>
public interface IQueryPagedOptionsBase : IQueryOptions
{
    /// <summary>
    /// Number of items per page.
    /// </summary>
    int? PageSize { get; set; }
}
