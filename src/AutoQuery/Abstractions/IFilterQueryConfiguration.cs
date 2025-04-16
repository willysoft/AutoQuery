namespace AutoQuery.Abstractions;

/// <summary>
/// 定義篩選查詢配置的介面。
/// </summary>
/// <typeparam name="TQueryOptions">查詢選項的類型。</typeparam>
/// <typeparam name="TData">數據的類型。</typeparam>
public interface IFilterQueryConfiguration<TQueryOptions, TData>
{
    /// <summary>
    /// 配置篩選查詢建構器。
    /// </summary>
    /// <param name="builder">篩選查詢建構器。</param>
    void Configure(FilterQueryBuilder<TQueryOptions, TData> builder);
}
