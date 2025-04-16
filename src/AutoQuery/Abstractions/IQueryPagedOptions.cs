namespace AutoQuery.Abstractions;

/// <summary>
/// 查詢已分頁參數
/// </summary>
public interface IQueryPagedOptions : IQueryOptions
{
    /// <summary>
    /// 當前頁數
    /// </summary>
    int? Page { get; set; }

    /// <summary>
    /// 每頁資料數量
    /// </summary>
    int? PageSize { get; set; }
}
