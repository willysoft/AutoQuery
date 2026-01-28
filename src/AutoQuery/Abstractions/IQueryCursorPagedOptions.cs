namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for cursor-based paginated results.
/// </summary>
public interface IQueryCursorPagedOptions : IQueryPagedOptionsBase
{
    /// <summary>
    /// Page token for cursor-based pagination.
    /// </summary>
    string? PageToken { get; set; }
}
