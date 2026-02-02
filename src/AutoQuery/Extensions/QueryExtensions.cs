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
    private static readonly MethodInfo s_StringCompareMethod = 
        typeof(string).GetMethod(nameof(string.Compare), new[] { typeof(string), typeof(string) })!;

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
    /// Applies query conditions and cursor-based pagination.
    /// </summary>
    /// <remarks>
    /// Supports sorting by any field using composite cursors.
    /// The page token encodes all sort field values plus the cursor key for accurate pagination.
    /// </remarks>
    /// <typeparam name="TData">The type of the entity being queried.</typeparam>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <param name="query">The query object.</param>
    /// <param name="queryProcessor">The query processor.</param>
    /// <param name="queryOption">The query options.</param>
    /// <returns>The cursor-based paginated result.</returns>
    public static CursorPagedResult<TData> ApplyQueryCursorPagedResult<TData, TQueryOptions>(
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

        var cursorKeySelector = queryProcessor.GetCursorKeySelector<TQueryOptions, TData>();
        if (cursorKeySelector == null)
            throw new InvalidOperationException($"Cursor key selector not configured for {typeof(TData).Name}. Use HasCursorKey() in your configuration.");

        var sortFields = ParseSortFields(queryOption.Sort);
        var cursorPropertyName = GetPropertyName(cursorKeySelector);
        
        if (!string.IsNullOrEmpty(cursorPropertyName) && 
            !sortFields.Any(sf => string.Equals(sf.PropertyName, cursorPropertyName, StringComparison.OrdinalIgnoreCase)))
        {
            sortFields.Add(new SortField { PropertyName = cursorPropertyName, IsDescending = false });
        }

        query = ApplySort(query, sortFields);

        if (!string.IsNullOrWhiteSpace(queryOption.PageToken))
        {
            query = ApplyCompositeCursorFilter(query, sortFields, queryOption.PageToken);
        }

        var pageSize = queryOption.PageSize ?? 10;
        var items = query.Take(pageSize + 1).ToList();

        string? nextPageToken = null;
        int count;
        
        if (items.Count > pageSize)
        {
            items.RemoveAt(pageSize);
            count = pageSize;
            nextPageToken = CreateCompositeCursorToken(items[^1], sortFields);
        }
        else
        {
            count = items.Count;
        }

        return new CursorPagedResult<TData>(items.AsQueryable(), nextPageToken, count);
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

        var sortFields = ParseSortFields(queryOption.Sort);
        return ApplySort(query, sortFields);
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
    /// Applies sort fields to query.
    /// </summary>
    /// <remarks>
    /// This private method provides the sorting implementation used by cursor pagination.
    /// It accepts a structured list of sort fields rather than a string expression.
    /// </remarks>
    private static IQueryable<TData> ApplySort<TData>(IQueryable<TData> query, List<SortField> sortFields)
    {
        if (sortFields.Count == 0)
            return query;

        var isFirstSort = true;

        foreach (var sortField in sortFields)
        {
            var cacheKey = $"{typeof(TData).FullName}_{sortField.PropertyName}";
            var propertyInfo = s_PropertyCache.GetOrAdd(cacheKey, _ =>
                typeof(TData).GetProperty(sortField.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance));

            if (propertyInfo == null)
                continue;

            var parameter = Expression.Parameter(typeof(TData), "entity");
            var property = Expression.Property(parameter, propertyInfo);
            var lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(TData), propertyInfo.PropertyType), property, parameter);

            string methodName = (isFirstSort, sortField.IsDescending) switch
            {
                (true, true) => "OrderByDescending",
                (true, false) => "OrderBy",
                (false, true) => "ThenByDescending",
                (false, false) => "ThenBy"
            };

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(TData), propertyInfo.PropertyType],
                query.Expression,
                lambda
            );

            query = query.Provider.CreateQuery<TData>(resultExpression);
            isFirstSort = false;
        }

        return query;
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

    /// <summary>
    /// Represents a sort field with its direction.
    /// </summary>
    private class SortField
    {
        public string PropertyName { get; set; } = null!;
        public bool IsDescending { get; set; }
    }

    /// <summary>
    /// Parses sort expression into list of sort fields.
    /// </summary>
    private static List<SortField> ParseSortFields(string? sortExpression)
    {
        if (string.IsNullOrWhiteSpace(sortExpression))
            return new List<SortField>();

        var fields = sortExpression.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sortFields = new List<SortField>(fields.Length);
        
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field))
                continue;

            var isDescending = field.StartsWith("-");
            var propertyName = isDescending ? field[1..] : field;

            sortFields.Add(new SortField 
            { 
                PropertyName = propertyName, 
                IsDescending = isDescending 
            });
        }

        return sortFields;
    }

    /// <summary>
    /// Creates a composite cursor token from the last item.
    /// </summary>
    private static string CreateCompositeCursorToken<TData>(TData lastItem, List<SortField> sortFields)
    {
        var cursorValues = new Dictionary<string, object?>(sortFields.Count);
        var typeName = typeof(TData).FullName!;

        foreach (var sortField in sortFields)
        {
            var cacheKey = $"{typeName}.{sortField.PropertyName}";
            var property = s_PropertyCache.GetOrAdd(cacheKey, _ =>
                typeof(TData).GetProperty(sortField.PropertyName, 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            if (property != null)
            {
                cursorValues[sortField.PropertyName] = property.GetValue(lastItem);
            }
        }

        if (cursorValues.Count == 0)
            throw new InvalidOperationException($"No valid properties found for sort fields in type {typeof(TData).Name}");

        return PageToken.EncodeComposite(cursorValues);
    }

    /// <summary>
    /// Applies composite cursor filter to the query.
    /// </summary>
    private static IQueryable<TData> ApplyCompositeCursorFilter<TData>(
        IQueryable<TData> query, 
        List<SortField> sortFields, 
        string pageToken)
    {
        if (sortFields.Count == 0)
            return query;

        var cursorValues = PageToken.DecodeComposite(pageToken);
        var parameter = Expression.Parameter(typeof(TData), "entity");
        Expression? filterExpression = null;

        for (int i = 0; i < sortFields.Count; i++)
        {
            Expression? currentCondition = null;

            for (int j = 0; j < i; j++)
            {
                var prevField = sortFields[j];
                if (!cursorValues.TryGetValue(prevField.PropertyName, out var prevCursorValue))
                    continue;

                var prevProperty = Expression.Property(parameter, prevField.PropertyName);
                var prevValue = ConvertJsonElement(prevCursorValue, prevProperty.Type);
                var prevConstant = Expression.Constant(prevValue, prevProperty.Type);
                var equality = Expression.Equal(prevProperty, prevConstant);

                currentCondition = currentCondition == null ? equality : Expression.AndAlso(currentCondition, equality);
            }

            var currentField = sortFields[i];
            if (cursorValues.TryGetValue(currentField.PropertyName, out var currentCursorValue))
            {
                var currentProperty = Expression.Property(parameter, currentField.PropertyName);
                var currentValue = ConvertJsonElement(currentCursorValue, currentProperty.Type);
                var currentConstant = Expression.Constant(currentValue, currentProperty.Type);
                
                Expression comparison;
                
                if (currentProperty.Type == typeof(string))
                {
                    var compareCall = Expression.Call(s_StringCompareMethod, currentProperty, currentConstant);
                    var zero = Expression.Constant(0);
                    
                    comparison = currentField.IsDescending
                        ? Expression.LessThan(compareCall, zero)
                        : Expression.GreaterThan(compareCall, zero);
                }
                else
                {
                    comparison = currentField.IsDescending
                        ? Expression.LessThan(currentProperty, currentConstant)
                        : Expression.GreaterThan(currentProperty, currentConstant);
                }

                currentCondition = currentCondition == null 
                    ? comparison 
                    : Expression.AndAlso(currentCondition, comparison);

                filterExpression = filterExpression == null 
                    ? currentCondition 
                    : Expression.OrElse(filterExpression, currentCondition);
            }
        }

        if (filterExpression != null)
        {
            var lambda = Expression.Lambda<Func<TData, bool>>(filterExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// Converts JsonElement to the target type.
    /// </summary>
    private static object? ConvertJsonElement(System.Text.Json.JsonElement jsonElement, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.String => jsonElement.GetString(),
            TypeCode.Int32 => jsonElement.GetInt32(),
            TypeCode.Int64 => jsonElement.GetInt64(),
            TypeCode.Boolean => jsonElement.GetBoolean(),
            TypeCode.Double => jsonElement.GetDouble(),
            TypeCode.Decimal => jsonElement.GetDecimal(),
            TypeCode.DateTime => jsonElement.GetDateTime(),
            _ => underlyingType == typeof(DateTimeOffset) ? jsonElement.GetDateTimeOffset() :
                 underlyingType == typeof(Guid) ? jsonElement.GetGuid() :
                 System.Text.Json.JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType)
        };
    }
}
