using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery;

/// <summary>
/// 用於構建篩選查詢屬性的建構器類別。
/// </summary>
/// <typeparam name="TData">數據的類型。</typeparam>
/// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
/// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
public class ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> : IFilterExpressionBuilder<TData, TQueryProperty>
{
    private Expression<Func<TData, TDataProperty>> _filterKeySelector;
    private List<(Expression<Func<TQueryProperty, TDataProperty, bool>> Expression, LogicalOperator Logical)> _filterExpressions = new();

    /// <summary>
    /// 初始化 <see cref="ComplexFilterQueryPropertyBuilder{TData, TQueryProperty, TDataProperty}"/> 類別的新實例。
    /// </summary>
    /// <param name="filterKeySelector">數據中的篩選鍵選擇器。</param>
    public ComplexFilterQueryPropertyBuilder(Expression<Func<TData, TDataProperty>> filterKeySelector)
    {
        _filterKeySelector = filterKeySelector;
    }

    /// <summary>
    /// 添加篩選表達式。
    /// </summary>
    /// <param name="filterExpression">篩選表達式。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
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