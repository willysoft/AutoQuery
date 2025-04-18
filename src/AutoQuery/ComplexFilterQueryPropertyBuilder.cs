using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery;

/// <summary>
/// A builder class for constructing filter query properties.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
/// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
/// <typeparam name="TDataProperty">The type of the data property.</typeparam>
public class ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> : IFilterExpressionBuilder<TData, TQueryProperty>
{
    private Expression<Func<TData, TDataProperty>> _filterKeySelector;
    private List<(Expression<Func<TQueryProperty, TDataProperty, bool>> Expression, LogicalOperator Logical)> _filterExpressions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexFilterQueryPropertyBuilder{TData, TQueryProperty, TDataProperty}"/> class.
    /// </summary>
    /// <param name="filterKeySelector">The filter key selector in the data.</param>
    public ComplexFilterQueryPropertyBuilder(Expression<Func<TData, TDataProperty>> filterKeySelector)
    {
        _filterKeySelector = filterKeySelector;
    }

    /// <summary>
    /// Adds a filter expression.
    /// </summary>
    /// <param name="filterExpression">The filter expression.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    public void AddFilterExpression(Expression<Func<TQueryProperty, TDataProperty, bool>> filterExpression, LogicalOperator logicalOperator)
    {
        _filterExpressions.Add((filterExpression, logicalOperator));
    }

    /// <inheritdoc />
    public Expression<Func<TData, bool>> BuildFilterExpression(TQueryProperty filterValue)
    {
        Expression<Func<TQueryProperty>> filterParameterLambda = () => filterValue;

        var parameter = Expression.Parameter(typeof(TData), "x");
        var keyBody = ExpressionExtensions.ReplaceParameter(_filterKeySelector.Parameters[0], parameter, _filterKeySelector.Body);

        Expression? finalExpression = null;

        foreach (var (expression, logical) in _filterExpressions)
        {
            var constantQuery = filterParameterLambda.Body;
            var replaced = ExpressionExtensions.ReplaceParameters(expression.Parameters[0], constantQuery, expression.Parameters[1], keyBody, expression.Body);

            finalExpression = finalExpression == null
                ? replaced
                : ExpressionExtensions.CombineExpressions(finalExpression, replaced, logical);
        }

        return Expression.Lambda<Func<TData, bool>>(finalExpression ?? Expression.Constant(true), parameter);
    }
}
