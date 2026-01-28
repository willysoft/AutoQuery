namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters for paginated results (supports both offset and cursor-based pagination).
/// </summary>
/// <remarks>
/// This interface combines both offset-based and cursor-based pagination options.
/// For new code, consider using <see cref="IQueryOffsetPagedOptions"/> or <see cref="IQueryCursorPagedOptions"/> 
/// for clearer separation of pagination modes.
/// </remarks>
public interface IQueryPagedOptions : IQueryOffsetPagedOptions, IQueryCursorPagedOptions
{
}
