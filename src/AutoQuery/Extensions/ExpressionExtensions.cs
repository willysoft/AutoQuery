using AutoQuery.Internal;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutoQuery.Extensions;

/// <summary>
/// Provides extension methods for working with expression trees.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Replaces a parameter in an expression.
    /// </summary>
    /// <param name="oldParam">The old parameter to replace.</param>
    /// <param name="newParam">The new parameter expression.</param>
    /// <param name="body">The body of the expression containing the parameter.</param>
    /// <returns>The expression with the parameter replaced.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression ReplaceParameter(ParameterExpression oldParam, Expression newParam, Expression body)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(body);
    }

    /// <summary>
    /// Replaces multiple parameters in an expression.
    /// </summary>
    /// <param name="oldParam1">The first old parameter to replace.</param>
    /// <param name="newParam1">The first new parameter expression.</param>
    /// <param name="oldParam2">The second old parameter to replace.</param>
    /// <param name="newParam2">The second new parameter expression.</param>
    /// <param name="body">The body of the expression containing the parameters.</param>
    /// <returns>The expression with the parameters replaced.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression ReplaceParameters(ParameterExpression oldParam1, Expression newParam1, ParameterExpression oldParam2, Expression newParam2, Expression body)
    {
        return new MultiParameterReplacer(oldParam1, newParam1, oldParam2, newParam2).Visit(body);
    }

    /// <summary>
    /// Combines two expressions using the specified logical operator.
    /// </summary>
    /// <param name="left">The left expression.</param>
    /// <param name="right">The right expression.</param>
    /// <param name="logical">The logical operator.</param>
    /// <returns>The combined expression.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression CombineExpressions(Expression left, Expression right, LogicalOperator logical)
    {
        return logical == LogicalOperator.OR ? Expression.OrElse(left, right) : Expression.AndAlso(left, right);
    }

    /// <summary>
    /// Gets the member access path.
    /// </summary>
    /// <param name="lambda">The lambda expression.</param>
    /// <param name="firstLevelOnly">Whether to allow only first-level members.</param>
    /// <param name="noError">Whether to return null instead of throwing an exception on error.</param>
    /// <returns>The member access path, or null if an error occurs and <paramref name="noError"/> is true.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="firstLevelOnly"/> is true and there are multiple levels of members, or if the expression is not a member access.</exception>
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
    /// Determines whether a lambda expression is an identity expression.
    /// </summary>
    /// <param name="lambda">The lambda expression.</param>
    /// <returns>True if it is an identity expression; otherwise, false.</returns>
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
    /// Trims conversion operations from an expression.
    /// </summary>
    /// <param name="exp">The expression to trim.</param>
    /// <param name="force">Whether to force trimming all conversion operations.</param>
    /// <returns>The trimmed expression.</returns>
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
    /// Determines whether a type can be reference-assigned from another type.
    /// </summary>
    /// <param name="destType">The destination type.</param>
    /// <param name="srcType">The source type.</param>
    /// <returns>True if reference assignment is possible; otherwise, false.</returns>
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
