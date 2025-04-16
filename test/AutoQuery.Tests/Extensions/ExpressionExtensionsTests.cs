using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery.Tests.Extensions;

public class ExpressionExtensionsTests
{
    [Fact]
    public void ReplaceParameter_ShouldReplaceParameter()
    {
        // Arrange
        var oldParam = Expression.Parameter(typeof(int), "x");
        var newParam = Expression.Constant(5);
        var body = Expression.Add(oldParam, Expression.Constant(1));

        // Act
        var result = ExpressionExtensions.ReplaceParameter(oldParam, newParam, body);

        // Assert
        var expected = Expression.Add(newParam, Expression.Constant(1));
        Assert.Equal(expected.ToString(), result.ToString());
    }

    [Fact]
    public void ReplaceParameters_ShouldReplaceMultipleParameters()
    {
        // Arrange
        var oldParam1 = Expression.Parameter(typeof(int), "x");
        var newParam1 = Expression.Constant(5);
        var oldParam2 = Expression.Parameter(typeof(int), "y");
        var newParam2 = Expression.Constant(3);
        var body = Expression.Add(oldParam1, oldParam2);

        // Act
        var result = ExpressionExtensions.ReplaceParameters(oldParam1, newParam1, oldParam2, newParam2, body);

        // Assert
        var expected = Expression.Add(newParam1, newParam2);
        Assert.Equal(expected.ToString(), result.ToString());
    }

    [Theory]
    [InlineData(LogicalOperator.AND, "AndAlso")]
    [InlineData(LogicalOperator.OR, "OrElse")]
    public void CombineExpressions_ShouldCombineExpressions(LogicalOperator logicalOperator, string expectedMethod)
    {
        // Arrange
        var left = Expression.Constant(true);
        var right = Expression.Constant(false);

        // Act
        var result = ExpressionExtensions.CombineExpressions(left, right, logicalOperator);

        // Assert
        Assert.Equal(expectedMethod, result.NodeType.ToString());
    }

    [Theory]
    [InlineData("Name", false, false, "Name")]
    [InlineData("Child.Name", false, false, "Child.Name")]
    [InlineData("Child.Name", true, true, null)]
    public void GetMemberPath_ShouldReturnCorrectPath(string expectedPath, bool firstLevelOnly, bool noError, string expected)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestData), "x");
        Expression body = parameter;
        foreach (var member in expectedPath.Split('.'))
            body = Expression.Property(body, member);
        var lambda = Expression.Lambda(body, parameter);

        // Act
        var result = lambda.GetMemberPath(firstLevelOnly, noError);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("x => x", true)]
    [InlineData("x => x.Name", false)]
    public void IsIdentity_ShouldReturnCorrectResult(string lambdaExpression, bool expected)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestData), "x");
        var body = lambdaExpression == "x => x"
                 ? (Expression)parameter
                 : Expression.Property(parameter, "Name");
        var lambda = Expression.Lambda(body, parameter);

        // Act
        var result = lambda.IsIdentity();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TrimConversion_ShouldTrimConversions(bool force)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(object), "x");
        var body = Expression.Convert(parameter, typeof(int));
        var lambda = Expression.Lambda(body, parameter);

        // Act
        var result = lambda.Body.TrimConversion(force);

        // Assert
        var expectedBody = force ? body.Operand : body;
        Assert.Equal(expectedBody.ToString(), result.ToString());
    }

    private class TestData
    {
        public string Name { get; set; } = null!;
        public TestData Child { get; set; } = null!;
    }
}
