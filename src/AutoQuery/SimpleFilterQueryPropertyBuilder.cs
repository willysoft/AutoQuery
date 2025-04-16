using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery;

/// <summary>
/// 用於構建自定義篩選查詢屬性的建構器類別。
/// </summary>
/// <typeparam name="TData">數據的類型。</typeparam>
/// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
public class SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> : IFilterExpressionBuilder<TData, TQueryProperty>
{
    private List<(Expression<Func<TQueryProperty, TData, bool>> Expression, LogicalOperator Logical)> _filterExpressions = new();

    /// <summary>
    /// 添加自定義篩選表達式。
    /// </summary>
    /// <param name="filterExpression">篩選表達式。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
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