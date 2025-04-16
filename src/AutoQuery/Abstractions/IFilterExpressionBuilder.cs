using System.Linq.Expressions;

namespace AutoQuery.Abstractions;

/// <summary>
/// 定義篩選查詢屬性建構器的介面。
/// </summary>
/// <typeparam name="TData">數據的類型。</typeparam>
/// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
public interface IFilterExpressionBuilder<TData, TQueryProperty>
{
    /// <summary>
    /// 構建篩選表達式。
    /// </summary>
    /// <param name="filterValue">篩選屬性的值。</param>
    /// <returns>篩選表達式。</returns>
    Expression<Func<TData, bool>> BuildFilterExpression(TQueryProperty filterValue);
}
