using System.Linq.Expressions;

namespace AutoQuery.Abstractions;

/// <summary>
/// 定義查詢建構器服務的介面。
/// </summary>
public interface IQueryProcessor
{
    /// <summary>
    /// 構建篩選表達式。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryOptions">查詢選項的類型。</typeparam>
    /// <param name="queryOptions">查詢選項。</param>
    /// <returns>篩選表達式，如果沒有篩選條件則為 null。</returns>
    Expression<Func<TData, bool>>? BuildFilterExpression<TData, TQueryOptions>(TQueryOptions queryOptions)
        where TQueryOptions : IQueryOptions;

    /// <summary>
    /// 構建選擇器表達式。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryOptions">查詢選項的類型。</typeparam>
    /// <param name="queryOptions">查詢選項。</param>
    /// <returns>選擇器表達式，如果沒有選擇條件則為 null。</returns>
    Expression<Func<TData, TData>>? BuildSelectorExpression<TData, TQueryOptions>(TQueryOptions queryOptions)
        where TQueryOptions : IQueryOptions;
}

