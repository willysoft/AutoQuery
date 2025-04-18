using AutoQuery.Extensions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoQuery;

/// <summary>
/// A builder class for constructing filter queries.
/// </summary>
/// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
/// <typeparam name="TData">The type of the data.</typeparam>
public class FilterQueryBuilder<TQueryOptions, TData>
{
    private readonly ConcurrentDictionary<string, (object Builder, Type QueryPropertyType)> _builderProperties = new();
    private readonly ConcurrentDictionary<(Type BuilderType, Type QueryPropertyType), Func<object, object, Expression<Func<TData, bool>>>> _compiledExpressionsCache = new();
    private readonly ConcurrentDictionary<PropertyInfo, Func<TQueryOptions, object>> _propertyAccessorsCache = new();
    private readonly Dictionary<string, PropertyInfo> _queryOptionsProperties = typeof(TQueryOptions).GetProperties().ToDictionary(p => p.Name);

    /// <summary>
    /// Registers a property for use in filter queries.
    /// </summary>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="propertyExpression">The property expression in the query options.</param>
    /// <param name="filterKeySelector">The filter key selector in the data.</param>
    /// <returns>The filter query property builder.</returns>
    /// <exception cref="ArgumentException">Thrown when the property expression is invalid.</exception>
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
    /// Registers a property for use in filter queries.
    /// </summary>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <param name="propertyExpression">The property expression in the query options.</param>
    /// <returns>The filter query property builder.</returns>
    /// <exception cref="ArgumentException">Thrown when the property expression is invalid.</exception>
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
    /// Builds the filter expression.
    /// </summary>
    /// <param name="instance">The value of the query options.</param>
    /// <returns>The filter expression, or null if no filter conditions exist.</returns>
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
    /// Retrieves the value of a property using expression trees.
    /// </summary>
    /// <param name="property">The property information.</param>
    /// <param name="instance">The instance of the query options.</param>
    /// <returns>The value of the property.</returns>
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
    /// Invokes the method to build the filter expression.
    /// </summary>
    /// <param name="builderObj">The builder object.</param>
    /// <param name="filterPropertyValue">The value of the filter property.</param>
    /// <param name="queryPropertyType">The type of the query property.</param>
    /// <returns>The filter expression.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the BuildFilterExpression method is not found.</exception>
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
    /// Combines two filter expressions.
    /// </summary>
    /// <param name="expr1">The first filter expression.</param>
    /// <param name="expr2">The second filter expression.</param>
    /// <returns>The combined filter expression.</returns>
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
