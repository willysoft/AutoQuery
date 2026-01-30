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

    /// <summary>
    /// Applies query conditions and cursor-based pagination.
    /// </summary>
    /// <typeparam name="TData">The type of the entity being queried.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryProcessor">The query processor.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The cursor-based paginated result.</returns>
    public static CursorPagedResult<TData> ApplyQueryCursorPaged<TData, TQueryOptions>(
        this IQueryable<TData> query, 
        IQueryProcessor queryProcessor, 
        TQueryOptions queryOption)
        where TQueryOptions : IQueryCursorOptions
        where TData : class
    {
        var filterExpression = queryProcessor.BuildFilterExpression<TData, TQueryOptions>(queryOption);
        var selectorExpression = queryProcessor.BuildSelectorExpression<TData, TQueryOptions>(queryOption);
        
        if (filterExpression != null)
            query = query.Where(filterExpression);
        
        if (selectorExpression != null)
            query = query.Select(selectorExpression);

        // Get cursor key selector from the query processor
        var cursorKeySelector = queryProcessor.GetCursorKeySelector<TQueryOptions, TData>();
        
        if (cursorKeySelector == null)
            throw new InvalidOperationException($"Cursor key selector not configured for {typeof(TData).Name}. Use HasCursorKey() in your configuration.");

        // Apply sorting - cursor pagination requires consistent ordering
        query = query.ApplySort(queryOption);

        // Apply cursor-based filtering if page token is provided
        if (!string.IsNullOrWhiteSpace(queryOption.PageToken))
        {
            // Determine if cursor key is sorted in descending order
            bool isDescending = IsCursorKeyDescending(queryOption.Sort, cursorKeySelector);
            query = ApplyCursorFilter(query, cursorKeySelector, queryOption.PageToken, isDescending);
        }

        // Fetch one extra item to determine if there are more results
        var pageSize = queryOption.PageSize ?? 10;
        var items = query.Take(pageSize + 1).ToList();

        // Determine if there are more results and generate next page token
        string? nextPageToken = null;
        var hasMore = items.Count > pageSize;
        
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
            var lastItem = items.Last();
            var cursorValue = GetCursorValue(lastItem, cursorKeySelector);
            if (cursorValue == null)
                throw new InvalidOperationException($"Cursor key value cannot be null for {typeof(TData).Name}.");
            nextPageToken = PageToken.Encode(cursorValue);
        }

        return new CursorPagedResult<TData>(items, nextPageToken, items.Count);
    }

    /// <summary>
    /// Applies cursor-based filtering to the query.
    /// </summary>
    private static IQueryable<TData> ApplyCursorFilter<TData>(
        IQueryable<TData> query, 
        LambdaExpression cursorKeySelector, 
        string pageToken,
        bool isDescending)
    {
        var returnType = ((cursorKeySelector.Body as MemberExpression)?.Type) 
            ?? ((cursorKeySelector.Body as UnaryExpression)?.Operand as MemberExpression)?.Type
            ?? typeof(object);

        var decodeMethod = typeof(PageToken).GetMethod(nameof(PageToken.Decode))!.MakeGenericMethod(returnType);
        var cursorValue = decodeMethod.Invoke(null, new object[] { pageToken });

        if (cursorValue == null)
            throw new InvalidOperationException("Decoded cursor value cannot be null.");

        // Build the filter expression: entity => entity.CursorKey > cursorValue (ascending) or entity => entity.CursorKey < cursorValue (descending)
        var parameter = Expression.Parameter(typeof(TData), "entity");
        var cursorProperty = Expression.Invoke(cursorKeySelector, parameter);
        var constant = Expression.Constant(cursorValue, returnType);
        
        // Use LessThan for descending order, GreaterThan for ascending order
        var comparison = isDescending 
            ? Expression.LessThan(cursorProperty, constant)
            : Expression.GreaterThan(cursorProperty, constant);
        
        var lambda = Expression.Lambda<Func<TData, bool>>(comparison, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// Gets the cursor value from an entity.
    /// </summary>
    private static object? GetCursorValue<TData>(TData entity, LambdaExpression cursorKeySelector)
    {
        var compiled = cursorKeySelector.Compile();
        return compiled.DynamicInvoke(entity);
    }

    /// <summary>
    /// Determines if the cursor key is sorted in descending order.
    /// </summary>
    private static bool IsCursorKeyDescending(string? sortExpression, LambdaExpression cursorKeySelector)
    {
        if (string.IsNullOrWhiteSpace(sortExpression))
            return false;

        // Get the cursor key property name
        var cursorPropertyName = GetPropertyName(cursorKeySelector);
        if (string.IsNullOrEmpty(cursorPropertyName))
            return false;

        // Parse sort fields
        var sortFields = sortExpression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        foreach (var sortField in sortFields)
        {
            if (string.IsNullOrWhiteSpace(sortField))
                continue;

            var isDescending = sortField.StartsWith("-");
            var fieldName = isDescending ? sortField[1..] : sortField;

            // Check if this sort field matches the cursor key (case-insensitive)
            if (string.Equals(fieldName, cursorPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return isDescending;
            }
        }

        // If cursor key is not in the sort expression, default to ascending
        return false;
    }

    /// <summary>
    /// Gets the property name from a lambda expression.
    /// </summary>
    private static string? GetPropertyName(LambdaExpression expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        
        if (expression.Body is UnaryExpression unaryExpression && 
            unaryExpression.Operand is MemberExpression operandMember)
        {
            return operandMember.Member.Name;
        }

        return null;
    }
}
