using AutoQuery.Abstractions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoQuery;

/// <summary>
/// Provides query processing services.
/// </summary>
public class QueryProcessor : IQueryProcessor
{
    internal readonly Dictionary<(Type QueryOptionsType, Type DataType), object> _builders = new();
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> s_PropertysCache = new();

    /// <inheritdoc />
    public Expression<Func<TData, bool>>? BuildFilterExpression<TData, TQueryOptions>(TQueryOptions queryOptions)
        where TQueryOptions : IQueryOptions
    {
        var filterQueryBuilder = GetFilterQueryBuilder<TQueryOptions, TData>();
        if (filterQueryBuilder == null)
            return null;
        return filterQueryBuilder.BuildFilterExpression(queryOptions);
    }

    /// <inheritdoc />
    public Expression<Func<TData, TData>>? BuildSelectorExpression<TData, TQueryOptions>(TQueryOptions queryOptions)
        where TQueryOptions : IQueryOptions
    {
        if (string.IsNullOrWhiteSpace(queryOptions.Fields))
            return null;

        var selectedFields = queryOptions.Fields.Split(',')
                                                .Select(f => f.Trim())
                                                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parameter = Expression.Parameter(typeof(TData), "entity");
        var bindings = new List<MemberAssignment>();
        var properties = s_PropertysCache.GetOrAdd(typeof(TData), t => t.GetProperties());

        foreach (var property in properties)
        {
            if (selectedFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                var propertyAccess = Expression.Property(parameter, property);
                bindings.Add(Expression.Bind(property, propertyAccess));
            }
        }

        var body = Expression.MemberInit(Expression.New(typeof(TData)), bindings);
        var selector = Expression.Lambda<Func<TData, TData>>(body, parameter);

        return selector;
    }

    /// <summary>
    /// Gets the filter query builder.
    /// </summary>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <returns>The filter query builder, or null if it does not exist.</returns>
    private FilterQueryBuilder<TQueryOptions, TData>? GetFilterQueryBuilder<TQueryOptions, TData>()
    {
        var key = (typeof(TQueryOptions), typeof(TData));
        if (!_builders.TryGetValue(key, out var builder))
            return null;

        return (FilterQueryBuilder<TQueryOptions, TData>)builder;
    }

    /// <summary>
    /// Adds a filter query builder.
    /// </summary>
    /// <typeparam name="TQueryOptions">The type of the query options.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <param name="builder">The filter query builder.</param>
    public void AddFilterQueryBuilder<TQueryOptions, TData>(FilterQueryBuilder<TQueryOptions, TData> builder)
    {
        var key = (typeof(TQueryOptions), typeof(TData));
        _builders[key] = builder;
    }

    /// <summary>
    /// Applies configurations from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public void ApplyConfigurationsFromAssembly(Assembly assembly)
    {
        var configurations = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFilterQueryConfiguration<,>)))
            .Select(Activator.CreateInstance);

        foreach (var configuration in configurations)
        {
            if (configuration == null)
                continue;

            var configureMethod = configuration.GetType().GetMethod("Configure");
            if (configureMethod != null)
            {
                var queryOptionsType = configureMethod.GetParameters()[0].ParameterType.GetGenericArguments()[0];
                var dataType = configureMethod.GetParameters()[0].ParameterType.GetGenericArguments()[1];
                var builderType = typeof(FilterQueryBuilder<,>).MakeGenericType(queryOptionsType, dataType);
                var builder = Activator.CreateInstance(builderType);
                if (builder == null)
                    continue;

                configureMethod.Invoke(configuration, new[] { builder });
                _builders[(queryOptionsType, dataType)] = builder;
            }
        }
    }
}
