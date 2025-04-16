using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoQuery.Extensions;

/// <summary>
/// 提供擴充方法來建立篩選查詢屬性建構器。
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
    /// 添加相等篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.Equal, logicalOperator);
    }

    /// <summary>
    /// 添加不相等篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasNotEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.NotEqual, logicalOperator);
    }

    /// <summary>
    /// 添加大於或等於篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasGreaterThanOrEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.GreaterThanOrEqual, logicalOperator);
    }

    /// <summary>
    /// 添加大於篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasGreaterThan<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.GreaterThan, logicalOperator);
    }

    /// <summary>
    /// 添加小於或等於篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasLessThanOrEqual<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.LessThanOrEqual, logicalOperator);
    }

    /// <summary>
    /// 添加小於篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasLessThan<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddComparisonFilter(builder, ExpressionType.LessThan, logicalOperator);
    }

    /// <summary>
    /// 添加集合包含篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
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
    /// 添加字符串包含篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasStringContains<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddStringFilter(builder, s_StringContains, logicalOperator);
    }

    /// <summary>
    /// 添加字符串開頭篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasStringStartsWith<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        return AddStringFilter(builder, s_StringStartsWith, logicalOperator);
    }

    /// <summary>
    /// 添加自定義篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="customFilter">自定義篩選表達式。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> HasCustomFilter<TData, TQueryProperty, TDataProperty>(
        this ComplexFilterQueryPropertyBuilder<TData, TQueryProperty, TDataProperty> builder,
        Expression<Func<TQueryProperty, TDataProperty, bool>> customFilter, LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        builder.AddFilterExpression(customFilter, logicalOperator);
        return builder;
    }

    /// <summary>
    /// 添加比較篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="comparisonType">比較類型。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
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
    /// 添加字符串篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="stringMethod">字符串方法。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
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
    /// 創建集合包含條件。
    /// </summary>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <typeparam name="TDataProperty">數據屬性的類型。</typeparam>
    /// <param name="parameterA">查詢屬性參數。</param>
    /// <param name="parameterB">數據屬性參數。</param>
    /// <returns>集合包含條件表達式。</returns>
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
                       ?? throw new Exception("未能找到 ICollection<TDataProperty>.Contains 方法。");
            var condition = Expression.Call(parameterA, method, valueB);
            return Expression.AndAlso(Expression.AndAlso(notNullA, hasValueB), condition);
        }
        else if (parameterB.Type.IsValueType)
        {
            var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
            var method = typeof(ICollection<TDataProperty>).GetMethod(nameof(ICollection<object>.Contains), new[] { typeof(TDataProperty) })
                       ?? throw new Exception("未能找到 ICollection<TDataProperty>.Contains 方法。");
            var condition = Expression.Call(parameterA, method, parameterB);
            return Expression.AndAlso(notNullA, condition);
        }
        else
        {
            var notNullA = Expression.NotEqual(parameterA, Expression.Default(typeof(TQueryProperty)));
            var notNullB = Expression.NotEqual(parameterB, Expression.Default(typeof(TDataProperty)));
            var method = typeof(ICollection<TDataProperty>).GetMethod(nameof(ICollection<object>.Contains), new[] { typeof(TDataProperty) })
                       ?? throw new Exception("未能找到 ICollection<TDataProperty>.Contains 方法。");
            var condition = Expression.Call(parameterA, method, parameterB);
            return Expression.AndAlso(Expression.AndAlso(notNullA, notNullB), condition);
        }
    }

    /// <summary>
    /// 創建比較條件。
    /// </summary>
    /// <param name="parameterA">查詢屬性參數。</param>
    /// <param name="parameterB">數據屬性參數。</param>
    /// <param name="comparisonType">比較類型。</param>
    /// <returns>比較條件表達式。</returns>
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
