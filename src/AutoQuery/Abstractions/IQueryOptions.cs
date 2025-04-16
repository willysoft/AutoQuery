namespace AutoQuery.Abstractions;

/// <summary>
/// 查詢參數
/// </summary>
public interface IQueryOptions
{
    /// <summary>
    /// 選擇的欄位
    /// </summary>
    string? Fields { get; set; }

    /// <summary>
    /// 排序欄位 
    /// </summary>
    string? Sort { get; set; }
}

