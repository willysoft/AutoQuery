using AutoQuery.Extensions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoQuery;

/// <summary>
/// 用於構建篩選查詢的建構器類別。
/// </summary>
/// <typeparam name="TQueryOptions">查詢選項的類型。</typeparam>
/// <typeparam name="TData">數據的類型。</typeparam>
public class FilterQueryBuilder<TQueryOptions, TData>
{
    private readonly ConcurrentDictionary<string, (object Builder, Type QueryPropertyType)> _builderProperties = new();
    private readonly ConcurrentDictionary<(Type BuilderType, Type QueryPropertyType), Func<object, object, Expression<Func<TData, bool>>>> _compiledExpressionsCache = new();
    private readonly ConcurrentDictionary<PropertyInfo, Func<TQueryOptions, object>> _propertyAccessorsCache = new();
    private readonly Dictionary<string, PropertyInfo> _queryOptionsProperties = typeof(TQueryOptions).GetProperties().ToDictionary(p => p.Name);

    /// <summary>
    /// 註冊一個屬性以用於篩選查詢。
    /// </summary>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="propertyExpression">查詢選項中的屬性表達式。</param>
    /// <param name="filterKeySelector">數據中的篩選鍵選擇器。</param>
    /// <returns>篩選查詢屬性建構器。</returns>
    /// <exception cref="ArgumentException">當屬性表達式無效時拋出。</exception>
    public ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> Property<TQueryProperty, TDataProperty>(
        Expression<Func<TQueryOptions, TQueryProperty>> propertyExpression,
        Expression<Func<TData, TDataProperty>> filterKeySelector)
    {
        var memberPath = propertyExpression.GetMemberPath(firstLevelOnly: true, noError: false);
        if (memberPath == null)
            throw new ArgumentException("Invalid property expression");

        var builder = new ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty>(filterKeySelector);
        _builderProperties[memberPath] = (builder, typeof(TQueryProperty));
        return builder;
    }

    /// <summary>
    /// 註冊一個屬性以用於篩選查詢。
    /// </summary>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <param name="propertyExpression">查詢選項中的屬性表達式。</param>
    /// <returns>篩選查詢屬性建構器。</returns>
    /// <exception cref="ArgumentException">當屬性表達式無效時拋出。</exception>
    public SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> Property<TQueryProperty>(Expression<Func<TQueryOptions, TQueryProperty>> propertyExpression)
    {
        var memberPath = propertyExpression.GetMemberPath(firstLevelOnly: true, noError: false);
        if (memberPath == null)
            throw new ArgumentException("Invalid property expression");

        var builder = new SimpleFilterQueryPropertyBuilder<TData, TQueryProperty>();
        _builderProperties[memberPath] = (builder, typeof(TQueryProperty));
        return builder;
    }

    /// <summary>
    /// 構建篩選表達式。
    /// </summary>
    /// <param name="instance">查詢選項的值。</param>
    /// <returns>篩選表達式，如果沒有篩選條件則為 null。</returns>
    public Expression<Func<TData, bool>>? BuildFilterExpression(TQueryOptions instance)
    {
        Expression<Func<TData, bool>>? combinedExpression = null;

        foreach (var builderProperties in _builderProperties)
        {
            var key = builderProperties.Key;
            var (builderObj, queryPropertyType) = builderProperties.Value;
            if (_queryOptionsProperties.TryGetValue(key, out var property))
            {
                var filterPropertyValue = GetPropertyValue(property, instance);
                if (filterPropertyValue != null)
                {
                    var filterExpression = InvokeBuildFilterExpression(builderObj, filterPropertyValue, queryPropertyType);
                    if (filterExpression != null)
                    {
                        combinedExpression = combinedExpression == null
                                           ? filterExpression
                                           : CombineExpressions(combinedExpression, filterExpression);
                    }
                }
            }
        }

        return combinedExpression;
    }

    /// <summary>
    /// 使用表達樹取得屬性的值。
    /// </summary>
    /// <param name="property">屬性信息。</param>
    /// <param name="instance">查詢選項的實例。</param>
    /// <returns>屬性的值。</returns>
    private object? GetPropertyValue(PropertyInfo property, TQueryOptions instance)
    {
        if (!_propertyAccessorsCache.TryGetValue(property, out var accessor))
        {
            var parameter = Expression.Parameter(typeof(TQueryOptions), "instance");
            var propertyAccess = Expression.Property(parameter, property);
            var convert = Expression.Convert(propertyAccess, typeof(object));
            accessor = Expression.Lambda<Func<TQueryOptions, object>>(convert, parameter).Compile();
            _propertyAccessorsCache[property] = accessor;
        }

        return accessor(instance);
    }

    /// <summary>
    /// 調用篩選表達式的構建方法。
    /// </summary>
    /// <param name="builderObj">建構器對象。</param>
    /// <param name="filterPropertyValue">篩選屬性的值。</param>
    /// <param name="queryPropertyType">查詢屬性的類型。</param>
    /// <returns>篩選表達式。</returns>
    /// <exception cref="InvalidOperationException">當找不到 BuildFilterExpression 方法時拋出。</exception>
    private Expression<Func<TData, bool>>? InvokeBuildFilterExpression(object builderObj, object filterPropertyValue, Type queryPropertyType)
    {
        var builderType = builderObj.GetType();
        var cacheKey = (BuilderType: builderType, QueryPropertyType: queryPropertyType);

        if (!_compiledExpressionsCache.TryGetValue(cacheKey, out var compiledExpression))
        {
            var method = builderType.GetMethod("BuildFilterExpression", new[] { queryPropertyType });
            if (method == null)
                throw new InvalidOperationException("BuildFilterExpression method not found.");

            var builderParameter = Expression.Parameter(typeof(object), "builder");
            var filterValueParameter = Expression.Parameter(typeof(object), "filterValue");

            var builderCast = Expression.Convert(builderParameter, builderType);
            var filterValueCast = Expression.Convert(filterValueParameter, queryPropertyType);

            var call = Expression.Call(builderCast, method, filterValueCast);
            var lambda = Expression.Lambda<Func<object, object, Expression<Func<TData, bool>>>>(call, builderParameter, filterValueParameter);

            compiledExpression = lambda.Compile();
            _compiledExpressionsCache[cacheKey] = compiledExpression;
        }

        return compiledExpression(builderObj, filterPropertyValue);
    }

    /// <summary>
    /// 合併兩個篩選表達式。
    /// </summary>
    /// <param name="expr1">第一個篩選表達式。</param>
    /// <param name="expr2">第二個篩選表達式。</param>
    /// <returns>合併後的篩選表達式。</returns>
    private static Expression<Func<TData, bool>> CombineExpressions(Expression<Func<TData, bool>> expr1, Expression<Func<TData, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(TData));
        var body = Expression.AndAlso(
            ExpressionExtensions.ReplaceParameter(expr1.Parameters[0], parameter, expr1.Body),
            ExpressionExtensions.ReplaceParameter(expr2.Parameters[0], parameter, expr2.Body)
        );
        return Expression.Lambda<Func<TData, bool>>(body, parameter);
    }
}