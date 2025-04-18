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
/// 字段投影屬性，用於在返回結果中僅包含指定的字段。
/// </summary>
public class EnableFieldProjectionAttribute : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _propertyInfoCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, JsonPropertyNameAttribute?> _jsonPropertyNameCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _propertyAccessorsCache = new();

    private IQueryOptions? _queryOptions;

    /// <summary>
    /// 在操作執行之前調用，從操作參數中提取 QueryOptions。
    /// </summary>
    /// <param name="context">操作執行上下文。</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _queryOptions = ExtractQueryOptions(context);
    }

    /// <summary>
    /// 在操作執行之後調用，根據指定的字段過濾返回結果。
    /// </summary>
    /// <param name="context">操作執行上下文。</param>
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
    /// 過濾結果，僅包含選擇的字段。
    /// </summary>
    /// <param name="value">要過濾的對象。</param>
    /// <param name="selectedFields">選擇的字段集合。</param>
    /// <param name="serializerOptions">JSON 序列化選項。</param>
    /// <returns>過濾後的對象。</returns>
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
    /// 過濾集合類型的結果。
    /// </summary>
    /// <param name="enumerable">要過濾的集合。</param>
    /// <param name="selectedFields">選擇的字段集合。</param>
    /// <param name="serializerOptions">JSON 序列化選項。</param>
    /// <returns>過濾後的集合。</returns>
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
    /// 過濾單個物件的結果。
    /// </summary>
    /// <param name="value">要過濾的物件。</param>
    /// <param name="selectedFields">選擇的字段集合。</param>
    /// <param name="serializerOptions">JSON 序列化選項。</param>
    /// <param name="firstLevelOnly">是否僅過濾第一層屬性。</param>
    /// <returns>過濾後的字典。</returns>
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
    /// 從操作上下文中提取 QueryOptions。
    /// </summary>
    /// <param name="context">操作執行上下文。</param>
    /// <returns>提取的 QueryOptions。</returns>
    private static IQueryOptions? ExtractQueryOptions(ActionExecutingContext context)
    {
        return context.ActionArguments.Values.OfType<IQueryOptions>().FirstOrDefault();
    }

    /// <summary>
    /// 解析選擇的字段。
    /// </summary>
    /// <param name="fields">逗號分隔的字段名稱。</param>
    /// <returns>選擇的字段集合。</returns>
    private static HashSet<string> ParseSelectedFields(string fields)
    {
        return fields.Split(',')
                     .Select(f => f.Trim())
                     .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 獲取 JSON 序列化選項。
    /// </summary>
    /// <param name="context">操作執行上下文。</param>
    /// <returns>JSON 序列化選項。</returns>
    private static JsonSerializerOptions GetSerializerOptions(ActionExecutedContext context)
    {
        return context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    /// <summary>
    /// 使用表達樹取得屬性的值。
    /// </summary>
    /// <param name="property">屬性信息。</param>
    /// <param name="instance">對象實例。</param>
    /// <returns>屬性的值。</returns>
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
    /// 獲取屬性的 JSON 屬性名稱。
    /// </summary>
    /// <param name="propInfo">屬性信息。</param>
    /// <param name="serializerOptions">JSON 序列化選項。</param>
    /// <returns>屬性的 JSON 名稱。</returns>
    private static string GetJsonPropertyName(PropertyInfo propInfo, JsonSerializerOptions serializerOptions)
    {
        var jsonPropertyNameAttr = _jsonPropertyNameCache[propInfo];
        return jsonPropertyNameAttr?.Name
               ?? serializerOptions.PropertyNamingPolicy?.ConvertName(propInfo.Name)
               ?? propInfo.Name;
    }

    /// <summary>
    /// 緩存類型的屬性信息。
    /// </summary>
    /// <param name="type">要緩存的類型。</param>
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
    /// 確定狀態碼是否表示成功。
    /// </summary>
    /// <param name="statusCode">HTTP 狀態碼。</param>
    /// <returns>如果狀態碼表示成功，則為 true；否則為 false。</returns>
    private static bool IsSuccessStatusCode(int statusCode)
    {
        return statusCode >= 200 && statusCode < 300;
    }
}
