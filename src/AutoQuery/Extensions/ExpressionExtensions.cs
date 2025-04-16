using AutoQuery.Internal;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoQuery.Extensions;

/// <summary>
/// 提供擴展方法來處理表達式樹。
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// 替換表達式中的參數。
    /// </summary>
    /// <param name="oldParam">要替換的舊參數。</param>
    /// <param name="newParam">新的參數表達式。</param>
    /// <param name="body">包含參數的表達式主體。</param>
    /// <returns>替換後的表達式。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression ReplaceParameter(ParameterExpression oldParam, Expression newParam, Expression body)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(body);
    }

    /// <summary>
    /// 替換表達式中的多個參數。
    /// </summary>
    /// <param name="oldParam1">要替換的舊參數1。</param>
    /// <param name="newParam1">新的參數表達式1。</param>
    /// <param name="oldParam2">要替換的舊參數2。</param>
    /// <param name="newParam2">新的參數表達式2。</param>
    /// <param name="body">包含參數的表達式主體。</param>
    /// <returns>替換後的表達式。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression ReplaceParameters(ParameterExpression oldParam1, Expression newParam1, ParameterExpression oldParam2, Expression newParam2, Expression body)
    {
        return new MultiParameterReplacer(oldParam1, newParam1, oldParam2, newParam2).Visit(body);
    }

    /// <summary>
    /// 組合兩個表達式，使用指定的邏輯運算符。
    /// </summary>
    /// <param name="left">左側表達式。</param>
    /// <param name="right">右側表達式。</param>
    /// <param name="logical">邏輯運算符。</param>
    /// <returns>組合後的表達式。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression CombineExpressions(Expression left, Expression right, LogicalOperator logical)
    {
        return logical == LogicalOperator.OR ? Expression.OrElse(left, right) : Expression.AndAlso(left, right);
    }

    /// <summary>
    /// 獲取成員訪問路徑。
    /// </summary>
    /// <param name="lambda">Lambda 表達式。</param>
    /// <param name="firstLevelOnly">是否僅允許第一層成員。</param>
    /// <param name="noError">是否在錯誤時返回 null 而不是拋出異常。</param>
    /// <returns>成員訪問路徑，如果有錯誤且 <paramref name="noError"/> 為 true，則返回 null。</returns>
    /// <exception cref="ArgumentException">當 <paramref name="firstLevelOnly"/> 為 true 且有多層成員時，或當表達式不是成員訪問時拋出。</exception>
    public static string? GetMemberPath(this LambdaExpression lambda, bool firstLevelOnly = false, bool noError = false)
    {
        List<string> list = new List<string>();
        Expression? expression = lambda.Body.TrimConversion(force: true);
        while (expression != null && expression.NodeType == ExpressionType.MemberAccess)
        {
            if (firstLevelOnly && list.Count > 0)
            {
                if (noError)
                {
                    return null;
                }

                throw new ArgumentException("Only first level members are allowed (eg. obj => obj.Child)", "lambda");
            }

            MemberExpression memberExpression = (MemberExpression)expression;
            list.Add(memberExpression.Member.Name);
            expression = memberExpression.Expression;
        }

        if (list.Count == 0 || expression == null || expression.NodeType != ExpressionType.Parameter)
        {
            if (noError)
            {
                return null;
            }

            throw new ArgumentException("Allow only member access (eg. obj => obj.Child.Name)", "lambda");
        }

        list.Reverse();
        return string.Join(".", list);
    }

    /// <summary>
    /// 判斷 Lambda 表達式是否為身份表達式。
    /// </summary>
    /// <param name="lambda">Lambda 表達式。</param>
    /// <returns>如果是身份表達式，返回 true；否則返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIdentity(this LambdaExpression lambda)
    {
        Expression expression = lambda.Body.TrimConversion(force: true);
        if (lambda.Parameters.Count == 1)
        {
            return lambda.Parameters[0] == expression;
        }

        return false;
    }

    /// <summary>
    /// 修剪表達式中的轉換操作。
    /// </summary>
    /// <param name="exp">要修剪的表達式。</param>
    /// <param name="force">是否強制修剪所有轉換操作。</param>
    /// <returns>修剪後的表達式。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression TrimConversion(this Expression exp, bool force = false)
    {
        while (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
        {
            UnaryExpression unaryExpression = (UnaryExpression)exp;
            if (!force && !unaryExpression.Type.IsReferenceAssignableFrom(unaryExpression.Operand.Type))
            {
                break;
            }

            exp = unaryExpression.Operand;
        }

        return exp;
    }

    /// <summary>
    /// 判斷類型是否可以從另一個類型引用分配。
    /// </summary>
    /// <param name="destType">目標類型。</param>
    /// <param name="srcType">源類型。</param>
    /// <returns>如果可以引用分配，返回 true；否則返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsReferenceAssignableFrom(this Type destType, Type srcType)
    {
        if (destType == srcType)
        {
            return true;
        }

        if (!destType.GetTypeInfo().IsValueType && !srcType.GetTypeInfo().IsValueType && destType.GetTypeInfo().IsAssignableFrom(srcType.GetTypeInfo()))
        {
            return true;
        }

        return false;
    }
}
