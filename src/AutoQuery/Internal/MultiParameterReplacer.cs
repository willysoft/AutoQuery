using System.Linq.Expressions;

namespace AutoQuery.Internal;

/// <summary>
/// The MultiParameterReplacer class is used to replace multiple parameters in an expression.
/// </summary>
internal class MultiParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _param1;
    private readonly Expression _replacement1;
    private readonly ParameterExpression _param2;
    private readonly Expression _replacement2;

    /// <summary>
    /// Initializes a new instance of the MultiParameterReplacer class.
    /// </summary>
    /// <param name="param1">The first parameter to replace.</param>
    /// <param name="replacement1">The replacement expression for the first parameter.</param>
    /// <param name="param2">The second parameter to replace.</param>
    /// <param name="replacement2">The replacement expression for the second parameter.</param>
    internal MultiParameterReplacer(ParameterExpression param1, Expression replacement1, ParameterExpression param2, Expression replacement2)
    {
        _param1 = param1;
        _replacement1 = replacement1;
        _param2 = param2;
        _replacement2 = replacement2;
    }

    /// <summary>
    /// Visits a parameter node and performs replacement.
    /// </summary>
    /// <param name="node">The parameter node to visit.</param>
    /// <returns>The expression after replacement.</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _param1) return _replacement1;
        if (node == _param2) return _replacement2;
        return base.VisitParameter(node);
    }
}
