namespace AutoQuery.Tests;

public class FilterQueryBuilderTests
{
    private readonly FilterQueryBuilder<TestQueryOptions, TestData> _builder;

    public FilterQueryBuilderTests()
    {
        _builder = new FilterQueryBuilder<TestQueryOptions, TestData>();
    }

    [Fact]
    public void Property_ShouldRegisterComplexFilterQueryPropertyBuilder()
    {
        // Act
        var result = _builder.Property(x => x.Id, x => x.Id);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Property_ShouldRegisterSimpleFilterQueryPropertyBuilder()
    {
        // Act
        var result = _builder.Property(x => x.Name);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void BuildFilterExpression_ShouldReturnNull_WhenNoFiltersAreAdded()
    {
        // Act
        var result = _builder.BuildFilterExpression(new TestQueryOptions());

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void BuildFilterExpression_ShouldBuildCorrectExpression(int queryId, int dataId, bool expected)
    {
        // Arrange
        var complexBuilder = _builder.Property(x => x.Id, x => x.Id);
        complexBuilder.AddFilterExpression((queryData, data) => queryData == data, LogicalOperator.AND);

        // Act
        var filter = _builder.BuildFilterExpression(new TestQueryOptions { Id = queryId });

        // Assert
        Assert.NotNull(filter);
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
        var complexBuilder = _builder.Property(x => x.Id, x => x.Id);
        complexBuilder.AddFilterExpression((queryData, data) => queryData == data, LogicalOperator.AND);
        complexBuilder.AddFilterExpression((queryData, data) => queryData > data, LogicalOperator.OR);

        // Act
        var filter = _builder.BuildFilterExpression(new TestQueryOptions { Id = queryId });

        // Assert
        Assert.NotNull(filter);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataId }));
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private class TestQueryOptions
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
