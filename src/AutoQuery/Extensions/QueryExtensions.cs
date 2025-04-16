using AutoQuery.Abstractions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoQuery.Extensions;

/// <summary>
/// 提供查詢擴展方法。
/// </summary>
public static class QueryExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> s_PropertysCache = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo?> s_PropertyCache = new();

    /// <summary>
    /// 應用查詢條件。
    /// </summary>
    /// <typeparam name="TData">查詢的實體類型。</typeparam>
    /// <typeparam name="TQueryOptions">查詢選項的類型。</typeparam>
    /// <param name="query">查詢對象。</param>
    /// <param name="queryProcessor">查詢處理對象。</param>
    /// <param name="queryOption">查詢選項。</param>
    /// <returns>應用查詢條件後的查詢對象。</returns>
    public static IQueryable<TData> ApplyQuery<TData, TQueryOptions>(this IQueryable<TData> query, IQueryProcessor queryProcessor, TQueryOptions queryOption)
        where TQueryOptions : IQueryOptions
    {
        var filterExpression = queryProcessor.BuildFilterExpression<TData, TQueryOptions>(queryOption);
        var selectorExpression = queryProcessor.BuildSelectorExpression<TData, TQueryOptions>(queryOption);
        if (filterExpression != null)
            query = query.Where(filterExpression);
        if (selectorExpression != null)
            query = query.Select(selectorExpression);
        return query.ApplySort(queryOption);
    }

    /// <summary>
    /// 應用查詢條件和分頁選項。
    /// </summary>
    /// <typeparam name="TData">查詢的實體類型。</typeparam>
    /// <typeparam name="TQueryOptions">查詢選項的類型。</typeparam>
    /// <param name="query">查詢對象。</param>
    /// <param name="queryProcessor">查詢建構器。</param>
    /// <param name="queryOption">查詢選項。</param>
    /// <returns>應用查詢條件和分頁選項後的查詢對象。</returns>
    public static PagedResult<TData> ApplyQueryPaged<TData, TQueryOptions>(this IQueryable<TData> query, IQueryProcessor queryProcessor, TQueryOptions queryOption)
        where TQueryOptions : IQueryPagedOptions
        where TData : class
    {
        var filterExpression = queryProcessor.BuildFilterExpression<TData, TQueryOptions>(queryOption);
        var selectorExpression = queryProcessor.BuildSelectorExpression<TData, TQueryOptions>(queryOption);
        if (filterExpression != null)
            query = query.Where(filterExpression);
        if (selectorExpression != null)
            query = query.Select(selectorExpression);
        var count = query.Count();
        var page = queryOption.Page.HasValue ? queryOption.Page.Value : 1;
        var totalPages = queryOption.PageSize.HasValue
                       ? (int)Math.Ceiling((double)count / queryOption.PageSize.Value)
                       : 1;
        query = query.ApplySort(queryOption).ApplyPaging(queryOption);
        return new PagedResult<TData>(query, page, totalPages, count);
    }

    /// <summary>
    /// 對查詢結果應用排序。
    /// </summary>
    /// <typeparam name="T">查詢的實體類型。</typeparam>
    /// <param name="query">查詢對象。</param>
    /// <param name="queryOption">查詢選項。</param>
    /// <returns>排序後的查詢結果。</returns>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, IQueryOptions queryOption)
    {
        if (string.IsNullOrWhiteSpace(queryOption.Sort))
            return query;

        var sort = queryOption.Sort;
        var descending = sort.StartsWith("-");
        var sortBy = descending ? sort.Substring(1) : sort;
        var cacheKey = $"{typeof(T).FullName}_{sortBy}";
        var propertyInfo = s_PropertyCache.GetOrAdd(cacheKey, t => typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
        if (propertyInfo == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "entity");
        var property = Expression.Property(parameter, propertyInfo);
        var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propertyInfo.PropertyType);
        var lambda = Expression.Lambda(delegateType, property, parameter);

        string methodName = descending ? "OrderByDescending" : "OrderBy";
        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), propertyInfo.PropertyType],
            query.Expression,
            lambda
        );

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    /// <summary>
    /// 根據查詢條件進行分頁。
    /// </summary>
    /// <typeparam name="T">查詢的實體類型。</typeparam>
    /// <param name="query">查詢對象。</param>
    /// <param name="queryOption">查詢選項。</param>
    /// <returns>應用分頁後的查詢對象。</returns>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, IQueryPagedOptions queryOption)
    {
        if (queryOption.PageSize.HasValue)
        {
            var page = queryOption.Page.HasValue ? queryOption.Page.Value : 1;
            int skip = (page - 1) * queryOption.PageSize.Value;
            query = query.Skip(skip).Take(queryOption.PageSize.Value);
        }

        return query;
    }
}
