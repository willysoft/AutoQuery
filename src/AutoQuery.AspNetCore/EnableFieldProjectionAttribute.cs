using AutoQuery.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoQuery.AspNetCore;

/// <summary>
/// Field projection attribute used to include only specified fields in the returned result.
/// </summary>
public class EnableFieldProjectionAttribute : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _propertyInfoCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, JsonPropertyNameAttribute?> _jsonPropertyNameCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _propertyAccessorsCache = new();

    private IQueryOptions? _queryOptions;

    /// <summary>
    /// Called before the action is executed to extract QueryOptions from the action parameters.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _queryOptions = ExtractQueryOptions(context);
    }

    /// <summary>
    /// Called after the action is executed to filter the returned result based on the specified fields.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (_queryOptions == null || string.IsNullOrEmpty(_queryOptions.Fields))
            return;

        if (context.HttpContext.Response is HttpResponse response
            && IsSuccessStatusCode(response.StatusCode)
            && context.Result is ObjectResult objectResult
            && objectResult.Value != null)
        {
            var selectedFields = ParseSelectedFields(_queryOptions.Fields);
            var serializerOptions = GetSerializerOptions(context);
            objectResult.Value = FilterResult(objectResult.Value, selectedFields, serializerOptions);
        }
    }

    /// <summary>
    /// Filters the result to include only the selected fields.
    /// </summary>
    /// <param name="value">The object to filter.</param>
    /// <param name="selectedFields">The set of selected fields.</param>
    /// <param name="serializerOptions">JSON serialization options.</param>
    /// <returns>The filtered object.</returns>
    private static object FilterResult(object value, HashSet<string> selectedFields, JsonSerializerOptions serializerOptions)
    {
        if (value is IEnumerable<object> enumerable)
        {
            return FilterEnumerable(enumerable, selectedFields, serializerOptions);
        }
        else if (value.GetType().IsClass && !(value is string))
        {
            return FilterObject(value, selectedFields, serializerOptions);
        }
        else
        {
            return value;
        }
    }

    /// <summary>
    /// Filters a collection-type result.
    /// </summary>
    /// <param name="enumerable">The collection to filter.</param>
    /// <param name="selectedFields">The set of selected fields.</param>
    /// <param name="serializerOptions">JSON serialization options.</param>
    /// <returns>The filtered collection.</returns>
    private static List<Dictionary<string, object?>> FilterEnumerable(IEnumerable<object> enumerable, HashSet<string> selectedFields, JsonSerializerOptions serializerOptions)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in enumerable)
        {
            var dict = FilterObject(item, selectedFields, serializerOptions, true);
            result.Add(dict);
        }
        return result;
    }

    /// <summary>
    /// Filters the result of a single object.
    /// </summary>
    /// <param name="value">The object to filter.</param>
    /// <param name="selectedFields">The set of selected fields.</param>
    /// <param name="serializerOptions">JSON serialization options.</param>
    /// <param name="firstLevelOnly">Whether to filter only the first-level properties.</param>
    /// <returns>The filtered dictionary.</returns>
    private static Dictionary<string, object?> FilterObject(object value, HashSet<string> selectedFields, JsonSerializerOptions serializerOptions, bool firstLevelOnly = false)
    {
        var result = new Dictionary<string, object?>();
        var itemType = value.GetType();
        CacheProperties(itemType);

        foreach (var prop in _propertyInfoCache[itemType].Values)
        {
            var propValue = GetPropertyValue(prop, value);
            if (firstLevelOnly == false && propValue is IEnumerable<object>)
            {
                result.Clear();
                foreach (var siblingProp in _propertyInfoCache[itemType].Values)
                {
                    var siblingPropName = GetJsonPropertyName(siblingProp, serializerOptions);
                    var siblingPropValue = GetPropertyValue(siblingProp, value);
                    if (siblingPropValue is IEnumerable<object> enumerable)
                        result[siblingPropName] = FilterEnumerable(enumerable, selectedFields, serializerOptions);
                    else
                        result[siblingPropName] = siblingPropValue;
                }

                return result;
            }
            else
            {
                var propName = GetJsonPropertyName(prop, serializerOptions);
                if (!selectedFields.Contains(propName))
                    continue;
                result[propName] = propValue;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts QueryOptions from the action context.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    /// <returns>The extracted QueryOptions.</returns>
    private static IQueryOptions? ExtractQueryOptions(ActionExecutingContext context)
    {
        return context.ActionArguments.Values.OfType<IQueryOptions>().FirstOrDefault();
    }

    /// <summary>
    /// Parses the selected fields.
    /// </summary>
    /// <param name="fields">Comma-separated field names.</param>
    /// <returns>The set of selected fields.</returns>
    private static HashSet<string> ParseSelectedFields(string fields)
    {
        return fields.Split(',')
                     .Select(f => f.Trim())
                     .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the JSON serialization options.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    /// <returns>The JSON serialization options.</returns>
    private static JsonSerializerOptions GetSerializerOptions(ActionExecutedContext context)
    {
        return context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    /// <summary>
    /// Gets the value of a property using expression trees.
    /// </summary>
    /// <param name="property">The property information.</param>
    /// <param name="instance">The object instance.</param>
    /// <returns>The value of the property.</returns>
    private static object? GetPropertyValue(PropertyInfo property, object instance)
    {
        if (!_propertyAccessorsCache.TryGetValue(property, out var accessor))
        {
            var parameter = Expression.Parameter(typeof(object), "instance");
            var convertInstance = Expression.Convert(parameter, property.DeclaringType!);
            var propertyAccess = Expression.Property(convertInstance, property);
            var convert = Expression.Convert(propertyAccess, typeof(object));
            accessor = Expression.Lambda<Func<object, object>>(convert, parameter).Compile();
            _propertyAccessorsCache[property] = accessor;
        }

        return accessor(instance);
    }

    /// <summary>
    /// Gets the JSON property name of a property.
    /// </summary>
    /// <param name="propInfo">The property information.</param>
    /// <param name="serializerOptions">JSON serialization options.</param>
    /// <returns>The JSON name of the property.</returns>
    private static string GetJsonPropertyName(PropertyInfo propInfo, JsonSerializerOptions serializerOptions)
    {
        var jsonPropertyNameAttr = _jsonPropertyNameCache[propInfo];
        return jsonPropertyNameAttr?.Name
               ?? serializerOptions.PropertyNamingPolicy?.ConvertName(propInfo.Name)
               ?? propInfo.Name;
    }

    /// <summary>
    /// Caches the property information of a type.
    /// </summary>
    /// <param name="type">The type to cache.</param>
    private static void CacheProperties(Type type)
    {
        if (!_propertyInfoCache.ContainsKey(type))
        {
            var properties = new ConcurrentDictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                properties.TryAdd(prop.Name, prop);
                _jsonPropertyNameCache.TryAdd(prop, prop.GetCustomAttribute<JsonPropertyNameAttribute>());
            }

            _propertyInfoCache[type] = properties;
        }
    }

    /// <summary>
    /// Determines whether the status code indicates success.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if the status code indicates success; otherwise, false.</returns>
    private static bool IsSuccessStatusCode(int statusCode)
    {
        return statusCode >= 200 && statusCode < 300;
    }
}
