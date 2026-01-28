namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for offset-based paginated results.
/// </summary>
public interface IQueryOffsetPagedOptions : IQueryPagedOptionsBase
{
    /// <summary>
    /// Current page number (starting from 1).
    /// </summary>
    int? Page { get; set; }
}
