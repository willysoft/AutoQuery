using System.Linq.Expressions;

namespace AutoQuery.Internal;

/// <summary>
/// A parameter replacer used to replace parameters in an expression tree.
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly Expression _newParam;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterReplacer"/> class.
    /// </summary>
    /// <param name="oldParam">The old parameter to be replaced.</param>
    /// <param name="newParam">The new expression to replace with.</param>
    internal ParameterReplacer(ParameterExpression oldParam, Expression newParam)
    {
        _oldParam = oldParam;
        _newParam = newParam;
    }

    /// <summary>
    /// Visits and potentially modifies a parameter expression.
    /// </summary>
    /// <param name="node">The parameter expression to visit.</param>
    /// <returns>The replaced expression, or the original expression if no replacement occurred.</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}
