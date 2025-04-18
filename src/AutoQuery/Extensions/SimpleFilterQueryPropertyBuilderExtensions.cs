using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoQuery.Extensions;

/// <summary>
/// Provides extension methods for creating filter query property builders.
/// </summary>
public static class SimpleFilterQueryPropertyBuilderExtensions
{
    private static readonly MethodInfo s_StringContains;
    private static readonly MethodInfo s_StringStartsWith;

    static SimpleFilterQueryPropertyBuilderExtensions()
    {
        s_StringContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
            ?? throw new Exception("Failed to find string.Contains method.");
        s_StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })
            ?? throw new Exception("Failed to find string.StartsWith method.");
    }

    /// <summary>
    /// Adds a string contains filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="filterKeySelector">The filter key selector in the data.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> HasStringContains<TData, TQueryProperty>(
        this SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TData, TQueryProperty>> filterKeySelector,
        LogicalOperator logicalOperator = LogicalOperator.AND)
        where TQueryProperty : IComparable<string>?
    {
        return AddStringFilter(builder, filterKeySelector, s_StringContains, logicalOperator);
    }

    /// <summary>
    /// Adds a string starts-with filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="filterKeySelector">The filter key selector in the data.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> HasStringStartsWith<TData, TQueryProperty>(
        this SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TData, TQueryProperty>> filterKeySelector,
        LogicalOperator logicalOperator = LogicalOperator.AND)
        where TQueryProperty : IComparable<string>?
    {
        return AddStringFilter(builder, filterKeySelector, s_StringStartsWith, logicalOperator);
    }

    /// <summary>
    /// Adds a custom filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="customFilter">The custom filter logic.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> HasCustomFilter<TData, TQueryProperty>(
        this SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TQueryProperty, TData, bool>> customFilter,
        LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        builder.AddFilterExpression(customFilter, logicalOperator);
        return builder;
    }

    /// <summary>
    /// Adds a string filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="filterKeySelector">The filter key selector in the data.</param>
    /// <param name="stringMethod">The string method.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> AddStringFilter<TData, TQueryProperty>(
        SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TData, TQueryProperty>> filterKeySelector,
        MethodInfo stringMethod,
        LogicalOperator logicalOperator)
    {
        var parameterA = Expression.Parameter(typeof(TQueryProperty), "a");
        var parameterB = Expression.Parameter(typeof(TData), "x");

        var keyBody = ExpressionExtensions.ReplaceParameter(filterKeySelector.Parameters[0], parameterB, filterKeySelector.Body);

        var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
        var notNullB = Expression.NotEqual(keyBody, Expression.Default(typeof(TQueryProperty)));

        var condition = Expression.Call(keyBody, stringMethod, parameterA);
        var finalCondition = Expression.AndAlso(Expression.AndAlso(notNullA, notNullB), condition);

        var lambda = Expression.Lambda<Func<TQueryProperty, TData, bool>>(finalCondition, parameterA, parameterB);
        builder.AddFilterExpression(lambda, logicalOperator);

        return builder;
    }
}
