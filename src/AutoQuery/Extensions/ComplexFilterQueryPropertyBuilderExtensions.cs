using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoQuery.Extensions;

/// <summary>
/// Provides extension methods for creating filter query property builders.
/// </summary>
public static class ComplexFilterQueryPropertyBuilderExtensions
{
    private static readonly MethodInfo s_StringContains;
    private static readonly MethodInfo s_StringStartsWith;

    static ComplexFilterQueryPropertyBuilderExtensions()
    {
        s_StringContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
            ?? throw new Exception("未能找到 string.Contains 方法。");
        s_StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })
            ?? throw new Exception("未能找到 string.StartsWith 方法。");
    }

    /// <summary>
    /// Adds an equality filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.Equal, logicalOperator);
    }

    /// <summary>
    /// Adds a not-equal filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasNotEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.NotEqual, logicalOperator);
    }

    /// <summary>
    /// Adds a greater-than-or-equal filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasGreaterThanOrEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.GreaterThanOrEqual, logicalOperator);
    }

    /// <summary>
    /// Adds a greater-than filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasGreaterThan<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.GreaterThan, logicalOperator);
    }

    /// <summary>
    /// Adds a less-than-or-equal filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasLessThanOrEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.LessThanOrEqual, logicalOperator);
    }

    /// <summary>
    /// Adds a less-than filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasLessThan<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.LessThan, logicalOperator);
    }

    /// <summary>
    /// Adds a collection contains filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasCollectionContains<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        var parameterA = Expression.Parameter(typeof(TQueryProperty), "a");
        var parameterB = Expression.Parameter(typeof(TDataProperty), "b");

        var finalCondition = CreateCollectionContainsCondition<TQueryProperty, TDataProperty>(parameterA, parameterB);
        var lambda = Expression.Lambda<Func<TQueryProperty, TDataProperty, bool>>(finalCondition, parameterA, parameterB);
        builder.AddFilterExpression(lambda, logicalOperator);

        return builder;
    }

    /// <summary>
    /// Adds a string contains filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasStringContains<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddStringFilter(builder, s_StringContains, logicalOperator);
    }

    /// <summary>
    /// Adds a string starts-with filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasStringStartsWith<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddStringFilter(builder, s_StringStartsWith, logicalOperator);
    }

    /// <summary>
    /// Adds a custom filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="customFilter">The custom filter expression.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasCustomFilter<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder,
        Expression<Func<TQueryProperty, TDataProperty, bool>> customFilter, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        builder.AddFilterExpression(customFilter, logicalOperator);
        return builder;
    }

    /// <summary>
    /// Adds a comparison filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="comparisonType">The type of comparison.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    private static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> AddComparisonFilter<TData, TQueryProperty, TDataProperty>(
        ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, ExpressionType comparisonType, LogicalOperator logicalOperator)
    {
        var parameterA = Expression.Parameter(typeof(TQueryProperty), "a");
        var parameterB = Expression.Parameter(typeof(TDataProperty), "b");

        var condition = CreateComparisonCondition(parameterA, parameterB, comparisonType);
        var lambda = Expression.Lambda<Func<TQueryProperty, TDataProperty, bool>>(condition, parameterA, parameterB);
        builder.AddFilterExpression(lambda, logicalOperator);
        return builder;
    }

    /// <summary>
    /// Adds a string filter condition.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="builder">The filter query property builder.</param>
    /// <param name="stringMethod">The string method.</param>
    /// <param name="logicalOperator">The logical operator.</param>
    /// <returns>The updated filter query property builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> AddStringFilter<TData, TQueryProperty, TDataProperty>(
        ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, MethodInfo stringMethod, LogicalOperator logicalOperator)
    {
        var parameterA = Expression.Parameter(typeof(TQueryProperty), "a");
        var parameterB = Expression.Parameter(typeof(TDataProperty), "b");

        var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
        var notNullB = Expression.NotEqual(parameterB, Expression.Default(typeof(TDataProperty)));

        var condition = Expression.Call(parameterB, stringMethod, parameterA);
        var finalCondition = Expression.AndAlso(Expression.AndAlso(notNullA, notNullB), condition);

        var lambda = Expression.Lambda<Func<TQueryProperty, TDataProperty, bool>>(finalCondition, parameterA, parameterB);
        builder.AddFilterExpression(lambda, logicalOperator);

        return builder;
    }

    /// <summary>
    /// Creates a collection contains condition.
    /// </summary>
    /// <typeparam name="TQueryProperty">The type of the query property.</typeparam>
    /// <typeparam name="TDataProperty">The type of the data property.</typeparam>
    /// <param name="parameterA">The query property parameter.</param>
    /// <param name="parameterB">The data property parameter.</param>
    /// <returns>The collection contains condition expression.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BinaryExpression CreateCollectionContainsCondition<TQueryProperty, TDataProperty>(ParameterExpression parameterA, ParameterExpression parameterB)
    {
        if (Nullable.GetUnderlyingType(parameterB.Type) != null)
        {
            var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
            var hasValueB = Expression.Property(parameterB, "HasValue");
            var valueB = Expression.Property(parameterB, "Value");
            var valueTypeB = valueB.Type;
            var method = typeof(ICollection<>).MakeGenericType(valueTypeB).GetMethod(nameof(ICollection<object>.Contains), new[] { valueTypeB })
                       ?? throw new Exception("Failed to find ICollection<TDataProperty>.Contains method.");
            var condition = Expression.Call(parameterA, method, valueB);
            return Expression.AndAlso(Expression.AndAlso(notNullA, hasValueB), condition);
        }
        else if (parameterB.Type.IsValueType)
        {
            var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
            var method = typeof(ICollection<TDataProperty>).GetMethod(nameof(ICollection<object>.Contains), new[] { typeof(TDataProperty) })
                       ?? throw new Exception("Failed to find ICollection<TDataProperty>.Contains method.");
            var condition = Expression.Call(parameterA, method, parameterB);
            return Expression.AndAlso(notNullA, condition);
        }
        else
        {
            var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
            var notNullB = Expression.NotEqual(parameterB, Expression.Default(typeof(TDataProperty)));
            var method = typeof(ICollection<TDataProperty>).GetMethod(nameof(ICollection<object>.Contains), new[] { typeof(TDataProperty) })
                       ?? throw new Exception("Failed to find ICollection<TDataProperty>.Contains method.");
            var condition = Expression.Call(parameterA, method, parameterB);
            return Expression.AndAlso(Expression.AndAlso(notNullA, notNullB), condition);
        }
    }

    /// <summary>
    /// Creates a comparison condition.
    /// </summary>
    /// <param name="parameterA">The query property parameter.</param>
    /// <param name="parameterB">The data property parameter.</param>
    /// <param name="comparisonType">The type of comparison.</param>
    /// <returns>The comparison condition expression.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BinaryExpression CreateComparisonCondition(ParameterExpression parameterA, ParameterExpression parameterB, ExpressionType comparisonType)
    {
        if (Nullable.GetUnderlyingType(parameterA.Type) != null)
        {
            var hasValueA = Expression.Property(parameterA, "HasValue");
            var valueA = Expression.Property(parameterA, "Value");
            if (Nullable.GetUnderlyingType(parameterB.Type) != null)
            {
                var hasValueB = Expression.Property(parameterB, "HasValue");
                var valueB = Expression.Property(parameterB, "Value");
                return Expression.AndAlso(
                    Expression.AndAlso(hasValueA, hasValueB),
                    Expression.MakeBinary(comparisonType, valueB, valueA)
                );
            }
            else
            {
                return Expression.AndAlso(
                    hasValueA,
                    Expression.MakeBinary(comparisonType, parameterB, valueA)
                );
            }
        }
        else
        {
            if (Nullable.GetUnderlyingType(parameterB.Type) != null)
            {
                var hasValueB = Expression.Property(parameterB, "HasValue");
                var valueB = Expression.Property(parameterB, "Value");
                return Expression.AndAlso(
                    hasValueB,
                    Expression.MakeBinary(comparisonType, valueB, parameterA)
                );
            }
            else
            {
                return Expression.MakeBinary(comparisonType, parameterB, parameterA);
            }
        }
    }
}
