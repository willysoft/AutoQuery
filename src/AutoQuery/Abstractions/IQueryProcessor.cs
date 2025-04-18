using System.Linq.Expressions;

namespace AutoQuery.Abstractions;

/// <summary>
/// Defines the interface for query builder services.
/// </summary>
public interface IQueryProcessor
{
    /// <summary>
    /// Builds the filter expression.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="queryOptions">The query options.</param>
    /// <returns>The filter expression, or null if no filter conditions exist.</returns>
    Expression<Func<TData, bool>>? BuildFilterExpression<TData, TQueryOptions>(TQueryOptions queryOptions)
        where TQueryOptions : IQueryOptions;

    /// <summary>
    /// Builds the selector expression.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="queryOptions">The query options.</param>
    /// <returns>The selector expression, or null if no selection conditions exist.</returns>
    Expression<Func<TData, TData>>? BuildSelectorExpression<TData, TQueryOptions>(TQueryOptions queryOptions)
        where TQueryOptions : IQueryOptions;
}

