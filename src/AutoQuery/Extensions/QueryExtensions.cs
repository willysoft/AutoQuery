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
        
        // If PageToken is provided, use cursor-based pagination
        if (!string.IsNullOrWhiteSpace(queryOption.PageToken))
        {
            return query.ApplySort(queryOption).ApplyCursorBasedPaging(queryOption);
        }
        
        // Otherwise, use traditional offset-based pagination
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
    /// Applies cursor-based pagination to the query results.
    /// </summary>
    /// <typeparam name="T">The type of the entity being queried.</typeparam>
    /// <param name="query">The query object (must already be sorted).</param>
    /// <param name="queryOption">The query options containing PageToken and PageSize.</param>
    /// <returns>A PagedResult with cursor tokens for navigation.</returns>
    /// <remarks>
    /// Important: This method requires the query to be sorted (using ApplySort or similar) before calling it.
    /// Without sorting, results may be inconsistent across page requests.
    /// Maximum page size is limited to 1000 for performance reasons.
    /// </remarks>
    public static PagedResult<T> ApplyCursorBasedPaging<T>(this IQueryable<T> query, IQueryPagedOptions queryOption)
        where T : class
    {
        // Validate and limit page size
        var pageSize = queryOption.PageSize ?? 10; // Default to 10 if not specified
        if (pageSize > 1000)
        {
            pageSize = 1000; // Cap at 1000 for safety
        }
        
        // Decode the page token if provided
        CursorData? cursorData = null;
        if (!string.IsNullOrWhiteSpace(queryOption.PageToken))
        {
            try
            {
                cursorData = PageToken.Decode<CursorData>(queryOption.PageToken);
            }
            catch (ArgumentException)
            {
                // Invalid token - start from the beginning
                cursorData = null;
            }
        }

        // Get the Id property once - assuming entities have an "Id" property
        var idProperty = s_PropertyCache.GetOrAdd(
            $"{typeof(T).FullName}_Id",
            _ => typeof(T).GetProperty("Id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));

        // Apply cursor filtering if we have cursor data and a valid Id property
        if (cursorData?.LastId != null && idProperty != null)
        {
            var parameter = Expression.Parameter(typeof(T), "entity");
            var property = Expression.Property(parameter, idProperty);
            
            // Handle nullable types
            var propertyType = idProperty.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            // Handle JsonElement from deserialization
            object convertedValue;
            if (cursorData.LastId is System.Text.Json.JsonElement jsonElement)
            {
                convertedValue = ConvertJsonElement(jsonElement, underlyingType);
            }
            else
            {
                convertedValue = Convert.ChangeType(cursorData.LastId, underlyingType);
            }
            
            // Create constant with the correct type
            var cursorIdValue = Expression.Constant(convertedValue, underlyingType);
            
            // Convert property to underlying type if it's nullable
            Expression propertyExpression = property;
            if (propertyType != underlyingType)
            {
                propertyExpression = Expression.Convert(property, underlyingType);
            }
            
            var comparison = Expression.GreaterThan(propertyExpression, cursorIdValue);
            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            query = query.Where(lambda);
        }

        // Fetch one extra item to determine if there's a next page
        var items = query.Take(pageSize + 1).ToList();
        var hasNextPage = items.Count > pageSize;
        
        // Remove the extra item if present
        if (hasNextPage)
        {
            items = items.Take(pageSize).ToList();
        }

        // Generate next page token
        string? nextPageToken = null;
        if (hasNextPage && items.Any() && idProperty != null)
        {
            var lastItem = items.Last();
            var lastId = idProperty.GetValue(lastItem);
            var newCursorData = new CursorData(lastId);
            nextPageToken = PageToken.Encode(newCursorData);
        }

        // For cursor-based pagination, we don't track total count or pages (for performance)
        return new PagedResult<T>(
            items.AsQueryable(), 
            Page: 0, // Not applicable for cursor-based pagination
            TotalPages: 0, // Not applicable for cursor-based pagination
            Count: 0, // Not applicable for cursor-based pagination
            NextPageToken: nextPageToken,
            PreviousPageToken: null // Previous tokens would require bi-directional cursor support
        );
    }

    /// <summary>
    /// Converts a JsonElement to the target type.
    /// </summary>
    private static object ConvertJsonElement(System.Text.Json.JsonElement element, Type targetType)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Number when targetType == typeof(int) => element.GetInt32(),
            System.Text.Json.JsonValueKind.Number when targetType == typeof(long) => element.GetInt64(),
            System.Text.Json.JsonValueKind.Number when targetType == typeof(double) => element.GetDouble(),
            System.Text.Json.JsonValueKind.Number when targetType == typeof(decimal) => element.GetDecimal(),
            System.Text.Json.JsonValueKind.String when targetType == typeof(string) => element.GetString() ?? "",
            System.Text.Json.JsonValueKind.String when targetType == typeof(Guid) => element.GetGuid(),
            System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False when targetType == typeof(bool) => element.GetBoolean(),
            _ => System.Text.Json.JsonSerializer.Deserialize(element.GetRawText(), targetType) ?? throw new InvalidCastException($"Cannot convert JsonElement to {targetType}")
        };
    }
}
