using System.Linq.Expressions;

namespace AutoQuery.Abstractions;

/// <summary>
/// Defines the interface for a filter query property builder.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
/// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
public interface IFilterExpressionBuilder<TData, TQueryProperty>
{
    /// <summary>
    /// Builds the filter expression.
    /// </summary>
    /// <param name="filterValue">The value of the filter property.</param>
    /// <returns>The filter expression.</returns>
    Expression<Func<TData, bool>> BuildFilterExpression(TQueryProperty filterValue);
}
