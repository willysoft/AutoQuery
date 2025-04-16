using AutoQuery.Internal;
using System.Linq.Expressions;

namespace AutoQuery.Tests.Internal;

public class ParameterReplacerTests
{
    [Fact]
    public void ShouldReplaceParameter()
    {
        // Arrange
        var oldParam = Expression.Parameter(typeof(int), "x");
        var newParam = Expression.Constant(5);
        var replacer = new ParameterReplacer(oldParam, newParam);

        var expression = Expression.Lambda<Func<int, int>>(
            Expression.Add(oldParam, Expression.Constant(3)),
            oldParam);

        // Act
        var replacedExpression = replacer.Visit(expression.Body);

        // Assert
        var expectedExpression = Expression.Add(newParam, Expression.Constant(3));
        Assert.Equal(expectedExpression.ToString(), replacedExpression.ToString());
    }

    [Fact]
    public void ShouldNotReplaceUnmatchedParameter()
    {
        // Arrange
        var oldParam = Expression.Parameter(typeof(int), "x");
        var newParam = Expression.Constant(5);
        var replacer = new ParameterReplacer(oldParam, newParam);

        var unmatchedParam = Expression.Parameter(typeof(int), "y");
        var expression = Expression.Lambda<Func<int, int, int>>(
            Expression.Add(unmatchedParam, Expression.Constant(3)),
            oldParam, unmatchedParam);

        // Act
        var replacedExpression = replacer.Visit(expression.Body);

        // Assert
        var expectedExpression = Expression.Add(unmatchedParam, Expression.Constant(3));
        Assert.Equal(expectedExpression.ToString(), replacedExpression.ToString());
    }

    [Fact]
    public void ShouldReplaceParameterInNestedExpression()
    {
        // Arrange
        var oldParam = Expression.Parameter(typeof(int), "x");
        var newParam = Expression.Constant(5);
        var replacer = new ParameterReplacer(oldParam, newParam);

        var nestedExpression = Expression.Lambda<Func<int, int>>(
            Expression.Add(
                Expression.Multiply(oldParam, Expression.Constant(2)),
                Expression.Constant(3)),
            oldParam);

        // Act
        var replacedExpression = replacer.Visit(nestedExpression.Body);

        // Assert
        var expectedExpression = Expression.Add(
            Expression.Multiply(newParam, Expression.Constant(2)),
            Expression.Constant(3));
        Assert.Equal(expectedExpression.ToString(), replacedExpression.ToString());
    }
}
