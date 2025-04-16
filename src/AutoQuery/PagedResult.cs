namespace AutoQuery;

/// <summary>
/// 表示分頁結果的泛型記錄類型。
/// </summary>
/// <typeparam name="TData">結果集中包含的數據類型。</typeparam>
/// <param name="Datas">分頁結果的數據集合，使用 <see cref="IQueryable{T}"/> 表示。</param>
/// <param name="Page">當前頁碼（從 1 開始）。</param>
/// <param name="TotalPages">總頁數。</param>
/// <param name="Count">結果集中數據的總數量。</param>
public record PagedResult<TData>(IQueryable<TData> Datas, int Page, int TotalPages, int Count);