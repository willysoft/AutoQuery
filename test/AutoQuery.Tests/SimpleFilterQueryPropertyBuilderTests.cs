using System.Linq.Expressions;

namespace AutoQuery.Tests;

public class SimpleFilterQueryPropertyBuilderTests
{
    private readonly SimpleFilterQueryPropertyBuilder<TestData, int> _builder;

    public SimpleFilterQueryPropertyBuilderTests()
    {
        _builder = new SimpleFilterQueryPropertyBuilder<TestData, int>();
    }

    [Theory]
    [InlineData(1)]
    public void AddFilterExpression_ShouldAddExpression(int queryId)
    {
        // Arrange
        Expression<Func<int, TestData, bool>> filterExpression = (queryProperty, data) => queryProperty == data.Id;

        // Act
        _builder.AddFilterExpression(filterExpression, LogicalOperator.AND);
        var filter = _builder.BuildFilterExpression(queryId);

        // Assert
        Assert.NotNull(filter);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void BuildFilterExpression_ShouldBuildCorrectExpression(int queryId, int dataId, bool expected)
    {
        // Arrange
        Expression<Func<int, TestData, bool>> filterExpression = (queryProperty, data) => queryProperty == data.Id;
        _builder.AddFilterExpression(filterExpression, LogicalOperator.AND);

        // Act
        var filter = _builder.BuildFilterExpression(queryId);

        // Assert
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataId }));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 0, true)]
    [InlineData(1, 2, false)]
    public void BuildFilterExpression_WithMultipleExpressions_ShouldCombineCorrectly(int queryId, int dataId, bool expected)
    {
        // Arrange
        Expression<Func<int, TestData, bool>> filterExpression1 = (queryProperty, data) => queryProperty == data.Id;
        Expression<Func<int, TestData, bool>> filterExpression2 = (queryProperty, data) => queryProperty > data.Id;
        _builder.AddFilterExpression(filterExpression1, LogicalOperator.AND);
        _builder.AddFilterExpression(filterExpression2, LogicalOperator.OR);

        // Act
        var filter = _builder.BuildFilterExpression(queryId);

        // Assert
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataId }));
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
