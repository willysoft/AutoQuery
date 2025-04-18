using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery;

/// <summary>
/// A builder class for constructing custom filter query properties.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
/// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
public class SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> : IFilterExpressionBuilder<TData, TQueryProperty>
{
    private List<(Expression<Func<TQueryProperty, TData, bool>> Expression, LogicalOperator Logical)> _filterExpressions = new();

    /// <summary>
    /// Adds a custom filter expression.
    /// </summary>
    /// <param name="filterExpression">The filter expression.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    public void AddFilterExpression(Expression<Func<TQueryProperty, TData, bool>> filterExpression, LogicalOperator logicalOperator)
    {
        _filterExpressions.Add((filterExpression, logicalOperator));
    }

    /// <inheritdoc />
    public Expression<Func<TData, bool>> BuildFilterExpression(TQueryProperty filterValue)
    {
        Expression<Func<TQueryProperty>> filterParameterLambda = () => filterValue;

        var parameter = Expression.Parameter(typeof(TData), "x");

        Expression? finalExpression = null;

        foreach (var (expression, logical) in _filterExpressions)
        {
            var constantQuery = filterParameterLambda.Body;
            var replaced = ExpressionExtensions.ReplaceParameters(expression.Parameters[0], constantQuery, expression.Parameters[1], parameter, expression.Body);

            finalExpression = finalExpression == null
                ? replaced
                : ExpressionExtensions.CombineExpressions(finalExpression, replaced, logical);
        }

        return Expression.Lambda<Func<TData, bool>>(finalExpression ?? Expression.Constant(true), parameter);
    }
}
