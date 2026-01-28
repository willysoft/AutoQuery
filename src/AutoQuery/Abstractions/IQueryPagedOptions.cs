namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for paginated results (offset-based pagination).
/// </summary>
/// <remarks>
/// This interface is kept for backward compatibility.
/// For new code, use <see cref="IQueryOffsetPagedOptions"/> for offset-based pagination
/// or <see cref="IQueryCursorPagedOptions"/> for cursor-based pagination.
/// </remarks>
[Obsolete("Use IQueryOffsetPagedOptions for offset pagination or IQueryCursorPagedOptions for cursor pagination. This interface will be removed in a future version.")]
public interface IQueryPagedOptions : IQueryOffsetPagedOptions
{
    /// <summary>
    /// Page token for cursor-based pagination. When provided, cursor-based pagination is used instead of offset-based.
    /// </summary>
    string? PageToken { get; set; }
}
