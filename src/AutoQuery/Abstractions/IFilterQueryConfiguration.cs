namespace AutoQuery.Abstractions;

/// <summary>
/// Defines the interface for filter query configuration.
/// </summary>
/// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
/// <typeparam name="TData">The type of the data.</typeparam>
public interface IFilterQueryConfiguration<TQueryOptions, TData>
{
    /// <summary>
    /// Configures the filter query builder.
    /// </summary>
    /// <param name="builder">The filter query builder.</param>
    void Configure(FilterQueryBuilder<TQueryOptions, TData> builder);
}
