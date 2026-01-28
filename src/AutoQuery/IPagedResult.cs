namespace AutoQuery;

/// <summary>
/// Base interface for paginated results.
/// </summary>
/// <typeparam name="TData">The type of data contained in the result set.</typeparam>
public interface IPagedResult<TData>
{
    /// <summary>
    /// The data collection of the paginated result.
    /// </summary>
    IQueryable<TData> Datas { get; }
}
