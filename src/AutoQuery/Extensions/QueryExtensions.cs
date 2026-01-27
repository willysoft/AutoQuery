using AutoQuery.Abstractions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoQuery.Extensions;

/// <summary>
/// Provides query extension methods.
/// </summary>
public static class QueryExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> s_PropertysCache = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo?> s_PropertyCache = new();

    /// <summary>
    /// Applies query conditions.
    /// </summary>
    /// <typeparam name="TData">The type of the entity being queried.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryProcessor">The query processor object.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The query object with conditions applied.</returns>
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
    /// Applies query conditions and pagination options.
    /// </summary>
    /// <typeparam name="TData">The type of the entity being queried.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryProcessor">The query processor.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The query object with conditions and pagination options applied.</returns>
    public static IQueryable<TData> ApplyQueryPaged<TData, TQueryOptions>(this IQueryable<TData> query, IQueryProcessor queryProcessor, TQueryOptions queryOption)
        where TQueryOptions : IQueryPagedOptions
        where TData : class
    {
        var filterExpression = queryProcessor.BuildFilterExpression<TData, TQueryOptions>(queryOption);
        var selectorExpression = queryProcessor.BuildSelectorExpression<TData, TQueryOptions>(queryOption);
        if (filterExpression != null)
            query = query.Where(filterExpression);
        if (selectorExpression != null)
            query = query.Select(selectorExpression);
        return query.ApplySort(queryOption).ApplyPaging(queryOption);
    }

    /// <summary>
    /// Applies query conditions and pagination options.
    /// </summary>
    /// <typeparam name="TData">The type of the entity being queried.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryProcessor">The query processor.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The query object with conditions and pagination options applied.</returns>
    public static PagedResult<TData> ApplyQueryPagedResult<TData, TQueryOptions>(this IQueryable<TData> query, IQueryProcessor queryProcessor, TQueryOptions queryOption)
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
    /// Applies sorting to the query results.
    /// </summary>
    /// <typeparam name="T">The type of the entity being queried.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The sorted query results.</returns>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, IQueryOptions queryOption)
    {
        if (string.IsNullOrWhiteSpace(queryOption.Sort))
            return query;

        var sortFields = queryOption.Sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var isFirstSort = true;

        foreach (var sort in sortFields)
        {
            if (string.IsNullOrWhiteSpace(sort))
                continue;

            var descending = sort.StartsWith("-");
            var sortBy = descending ? sort[1..] : sort;
            var cacheKey = $"{typeof(T).FullName}_{sortBy}";
            var propertyInfo = s_PropertyCache.GetOrAdd(cacheKey, _ => typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));

            if (propertyInfo == null)
                continue;

            var parameter = Expression.Parameter(typeof(T), "entity");
            var property = Expression.Property(parameter, propertyInfo);
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propertyInfo.PropertyType);
            var lambda = Expression.Lambda(delegateType, property, parameter);

            string methodName = (isFirstSort, descending) switch
            {
                (true, true) => "OrderByDescending",
                (true, false) => "OrderBy",
                (false, true) => "ThenByDescending",
                (false, false) => "ThenBy"
            };

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), propertyInfo.PropertyType],
                query.Expression,
                lambda
            );

            query = query.Provider.CreateQuery<T>(resultExpression);
            isFirstSort = false;
        }

        return query;
    }

    /// <summary>
    /// Applies pagination to the query results.
    /// </summary>
    /// <typeparam name="T">The type of the entity being queried.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The query object with pagination applied.</returns>
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
