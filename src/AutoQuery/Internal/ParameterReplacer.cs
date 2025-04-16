using System.Linq.Expressions;

namespace AutoQuery.Internal;

/// <summary>
/// 參數替換器，用於在表達式樹中替換參數。
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly Expression _newParam;

    /// <summary>
    /// 初始化 <see cref="ParameterReplacer"/> 類別的新執行個體。
    /// </summary>
    /// <param name="oldParam">要被替換的舊參數。</param>
    /// <param name="newParam">用來替換的新的表達式。</param>
    internal ParameterReplacer(ParameterExpression oldParam, Expression newParam)
    {
        _oldParam = oldParam;
        _newParam = newParam;
    }

    /// <summary>
    /// 訪問並可能修改參數表達式。
    /// </summary>
    /// <param name="node">要訪問的參數表達式。</param>
    /// <returns>替換後的表達式，如果沒有替換則返回原始表達式。</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}
