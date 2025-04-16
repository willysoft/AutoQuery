using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoQuery.Extensions;

/// <summary>
/// 提供擴充方法來建立篩選查詢屬性建構器。
/// </summary>
public static class SimpleFilterQueryPropertyBuilderExtensions
{
    private static readonly MethodInfo s_StringContains;
    private static readonly MethodInfo s_StringStartsWith;

    static SimpleFilterQueryPropertyBuilderExtensions()
    {
        s_StringContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
            ?? throw new Exception("未能找到 string.Contains 方法。");
        s_StringStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })
            ?? throw new Exception("未能找到 string.StartsWith 方法。");
    }

    /// <summary>
    /// 添加字符串包含篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="filterKeySelector">數據中的篩選鍵選擇器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> HasStringContains<TData, TQueryProperty>(
        this SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TData, TQueryProperty>> filterKeySelector,
        LogicalOperator logicalOperator = LogicalOperator.AND)
        where TQueryProperty : IComparable<string>?
    {
        return AddStringFilter(builder, filterKeySelector, s_StringContains, logicalOperator);
    }

    /// <summary>
    /// 添加字符串開頭篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="filterKeySelector">數據中的篩選鍵選擇器。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> HasStringStartsWith<TData, TQueryProperty>(
        this SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TData, TQueryProperty>> filterKeySelector,
        LogicalOperator logicalOperator = LogicalOperator.AND)
        where TQueryProperty : IComparable<string>?
    {
        return AddStringFilter(builder, filterKeySelector, s_StringStartsWith, logicalOperator);
    }

    /// <summary>
    /// 添加自定義篩選條件。
    /// </summary>
    /// <typeparam name="TData">資料型別。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性型別。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="customFilter">自定義篩選邏輯。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
    public static SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> HasCustomFilter<TData, TQueryProperty>(
        this SimpleFilterQueryPropertyBuilder<TData, TQueryProperty> builder,
        Expression<Func<TQueryProperty, TData, bool>> customFilter,
        LogicalOperator logicalOperator = LogicalOperator.AND)
    {
        builder.AddFilterExpression(customFilter, logicalOperator);
        return builder;
    }

    /// <summary>
    /// 添加字符串篩選條件。
    /// </summary>
    /// <typeparam name="TData">數據的類型。</typeparam>
    /// <typeparam name="TQueryProperty">查詢屬性的類型。</typeparam>
    /// <param name="builder">篩選查詢屬性建構器。</param>
    /// <param name="filterKeySelector">數據中的篩選鍵選擇器。</param>
    /// <param name="stringMethod">字符串方法。</param>
    /// <param name="logicalOperator">邏輯運算符。</param>
    /// <returns>更新後的篩選查詢屬性建構器。</returns>
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
