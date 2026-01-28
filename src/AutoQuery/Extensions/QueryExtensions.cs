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
            var cursorKeySelector = queryProcessor.GetCursorKeySelector<TQueryOptions, TData>();
            return query.ApplySort(queryOption).ApplyCursorBasedPaging(queryOption, cursorKeySelector);
        }
        
        // Otherwise, use traditional offset-based pagination
        var count = query.Count();
        var page = queryOption.Page.HasValue ? queryOption.Page.Value : 1;
        var totalPages = queryOption.PageSize.HasValue
                       ? (int)Math.Ceiling((double)count / queryOption.PageSize.Value)
                       : 1;
        var sortedQuery = query.ApplySort(queryOption);
        var pagedQuery = sortedQuery.ApplyPaging(queryOption);
        
        // Generate next page token if there are more pages and we have a cursor key configured
        string? nextPageToken = null;
        if (queryOption.PageSize.HasValue && page < totalPages)
        {
            var cursorKeySelector = queryProcessor.GetCursorKeySelector<TQueryOptions, TData>();
            nextPageToken = GeneratePageToken(pagedQuery, cursorKeySelector);
        }
        
        return new PagedResult<TData>(pagedQuery, page, totalPages, count, nextPageToken);
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
    /// <param name="cursorKeySelector">Optional cursor key selector. If not provided, uses "Id" property.</param>
    /// <returns>A PagedResult with cursor tokens for navigation.</returns>
    /// <remarks>
    /// Important: This method requires the query to be sorted (using ApplySort or similar) before calling it.
    /// Without sorting, results may be inconsistent across page requests.
    /// Maximum page size is limited to 1000 for performance reasons.
    /// </remarks>
    public static PagedResult<T> ApplyCursorBasedPaging<T>(this IQueryable<T> query, IQueryPagedOptions queryOption, Expression<Func<T, object>>? cursorKeySelector = null)
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

        // Determine cursor key property - use custom selector or default to "Id"
        PropertyInfo? cursorProperty = null;
        Expression? cursorExpression = null;
        
        if (cursorKeySelector != null)
        {
            // Extract property from the cursor key selector expression
            cursorExpression = cursorKeySelector.Body;
            // Remove Convert if present
            if (cursorExpression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                cursorExpression = unary.Operand;
            }
            if (cursorExpression is MemberExpression memberExpr && memberExpr.Member is PropertyInfo prop)
            {
                cursorProperty = prop;
            }
        }
        else
        {
            // Default to "Id" property
            cursorProperty = s_PropertyCache.GetOrAdd(
                $"{typeof(T).FullName}_Id",
                _ => typeof(T).GetProperty("Id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
        }

        // Apply cursor filtering if we have cursor data and a valid cursor property
        if (cursorData?.LastId != null && cursorProperty != null)
        {
            var parameter = Expression.Parameter(typeof(T), "entity");
            var property = Expression.Property(parameter, cursorProperty);
            
            // Handle nullable types
            var propertyType = cursorProperty.PropertyType;
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
            
            // Build comparison expression based on type
            Expression comparison;
            if (underlyingType == typeof(string))
            {
                // For strings, use String.CompareTo > 0
                var compareToMethod = typeof(string).GetMethod(nameof(string.CompareTo), new[] { typeof(string) })
                    ?? throw new InvalidOperationException("CompareTo method not found on string type.");
                var compareCall = Expression.Call(propertyExpression, compareToMethod, cursorIdValue);
                comparison = Expression.GreaterThan(compareCall, Expression.Constant(0));
            }
            else
            {
                // For numeric types, use direct GreaterThan
                comparison = Expression.GreaterThan(propertyExpression, cursorIdValue);
            }
            
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
        if (hasNextPage && items.Any() && cursorProperty != null)
        {
            var lastItem = items.Last();
            var lastId = cursorProperty.GetValue(lastItem);
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
    /// Generates a page token from the last item in a query result.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The query containing items.</param>
    /// <param name="cursorKeySelector">Optional cursor key selector. If not provided, uses "Id" property.</param>
    /// <returns>A page token string, or null if no items or cursor property found.</returns>
    private static string? GeneratePageToken<T>(IQueryable<T> query, Expression<Func<T, object>>? cursorKeySelector) where T : class
    {
        var items = query.ToList();
        if (!items.Any())
            return null;

        // Determine cursor key property
        PropertyInfo? cursorProperty = null;
        
        if (cursorKeySelector != null)
        {
            // Extract property from the cursor key selector expression
            var cursorExpression = cursorKeySelector.Body;
            // Remove Convert if present
            if (cursorExpression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                cursorExpression = unary.Operand;
            }
            if (cursorExpression is MemberExpression memberExpr && memberExpr.Member is PropertyInfo prop)
            {
                cursorProperty = prop;
            }
        }
        else
        {
            // Default to "Id" property
            cursorProperty = s_PropertyCache.GetOrAdd(
                $"{typeof(T).FullName}_Id",
                _ => typeof(T).GetProperty("Id", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));
        }

        if (cursorProperty == null)
            return null;

        var lastItem = items.Last();
        var lastId = cursorProperty.GetValue(lastItem);
        var cursorData = new CursorData(lastId);
        return PageToken.Encode(cursorData);
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
