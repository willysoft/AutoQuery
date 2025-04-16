using AutoQuery.Internal;
using System.Linq.Expressions;

namespace AutoQuery.Tests.Internal;

public class MultiParameterReplacerTests
{
    [Fact]
    public void ShouldReplaceSingleParameter()
    {
        // Arrange
        var param1 = Expression.Parameter(typeof(int), "x");
        var replacement1 = Expression.Constant(5);
        var param2 = Expression.Parameter(typeof(int), "y");
        var replacement2 = Expression.Constant(10);

        var replacer = new MultiParameterReplacer(param1, replacement1, param2, replacement2);

        var expression = Expression.Lambda<Func<int, int, int>>(
            Expression.Add(param1, param2),
            param1, param2);

        // Act
        var replacedExpression = replacer.Visit(expression.Body);

        // Assert
        var expectedExpression = Expression.Add(replacement1, replacement2);
        Assert.Equal(expectedExpression.ToString(), replacedExpression.ToString());
    }

    [Fact]
    public void ShouldReplaceMultipleParameters()
    {
        // Arrange
        var param1 = Expression.Parameter(typeof(int), "a");
        var replacement1 = Expression.Constant(3);
        var param2 = Expression.Parameter(typeof(int), "b");
        var replacement2 = Expression.Constant(7);

        var replacer = new MultiParameterReplacer(param1, replacement1, param2, replacement2);

        var expression = Expression.Lambda<Func<int, int, int>>(
            Expression.Multiply(
                Expression.Add(param1, param2),
                Expression.Subtract(param1, param2)),
            param1, param2);

        // Act
        var replacedExpression = replacer.Visit(expression.Body);

        // Assert
        var expectedExpression = Expression.Multiply(
            Expression.Add(replacement1, replacement2),
            Expression.Subtract(replacement1, replacement2));
        Assert.Equal(expectedExpression.ToString(), replacedExpression.ToString());
    }

    [Fact]
    public void ShouldNotReplaceUnmatchedParameters()
    {
        // Arrange
        var param1 = Expression.Parameter(typeof(int), "x");
        var replacement1 = Expression.Constant(5);
        var param2 = Expression.Parameter(typeof(int), "y");
        var replacement2 = Expression.Constant(10);

        var replacer = new MultiParameterReplacer(param1, replacement1, param2, replacement2);

        var param3 = Expression.Parameter(typeof(int), "z");
        var expression = Expression.Lambda<Func<int, int, int, int>>(
            Expression.Add(param1, param3),
            param1, param2, param3);

        // Act
        var replacedExpression = replacer.Visit(expression.Body);

        // Assert
        var expectedExpression = Expression.Add(replacement1, param3);
        Assert.Equal(expectedExpression.ToString(), replacedExpression.ToString());
    }
}