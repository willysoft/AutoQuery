using System.Linq.Expressions;

namespace AutoQuery.Internal;

/// <summary>
/// MultiParameterReplacer 類別用於替換表達式中的多個參數。
/// </summary>
internal class MultiParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _param1;
    private readonly Expression _replacement1;
    private readonly ParameterExpression _param2;
    private readonly Expression _replacement2;

    /// <summary>
    /// 初始化 MultiParameterReplacer 類別的新實例。
    /// </summary>
    /// <param name="param1">要替換的第一個參數。</param>
    /// <param name="replacement1">第一個參數的替換表達式。</param>
    /// <param name="param2">要替換的第二個參數。</param>
    /// <param name="replacement2">第二個參數的替換表達式。</param>
    internal MultiParameterReplacer(ParameterExpression param1, Expression replacement1, ParameterExpression param2, Expression replacement2)
    {
        _param1 = param1;
        _replacement1 = replacement1;
        _param2 = param2;
        _replacement2 = replacement2;
    }

    /// <summary>
    /// 訪問參數節點並進行替換。
    /// </summary>
    /// <param name="node">要訪問的參數節點。</param>
    /// <returns>替換後的表達式。</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _param1) return _replacement1;
        if (node == _param2) return _replacement2;
        return base.VisitParameter(node);
    }
}
