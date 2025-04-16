using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery.Tests.Extensions;

public class ComplexFilterQueryPropertyBuilderExtensionsTests
{
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void HasEqual_ShouldAddEqualFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);

        // Act
        var result = builder.HasEqual();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, true)]
    public void HasNotEqual_ShouldAddNotEqualFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);

        // Act
        var result = builder.HasNotEqual();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, true)]
    [InlineData(1, 0, false)]
    public void HasGreaterThanOrEqual_ShouldAddGreaterThanOrEqualFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);

        // Act
        var result = builder.HasGreaterThanOrEqual();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, true)]
    [InlineData(1, 0, false)]
    public void HasGreaterThan_ShouldAddGreaterThanFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);

        // Act
        var result = builder.HasGreaterThan();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(1, 0, true)]
    public void HasLessThanOrEqual_ShouldAddLessThanOrEqualFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);

        // Act
        var result = builder.HasLessThanOrEqual();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, false)]
    [InlineData(1, 0, true)]
    public void HasLessThan_ShouldAddLessThanFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);

        // Act
        var result = builder.HasLessThan();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    [Fact]
    public void HasCollectionContains_ShouldAddCollectionContainsFilter()
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, List<int>, int>(x => x.Id);

        // Act
        var result = builder.HasCollectionContains();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(new List<int> { 1, 2, 3 });
        var compiledFilter = filter.Compile();
        Assert.True(compiledFilter(new TestData { Id = 1 }));
        Assert.False(compiledFilter(new TestData { Id = 4 }));
    }

    [Theory]
    [InlineData("test", "this is a test", true)]
    [InlineData("test", "no match", false)]
    public void HasStringContains_ShouldAddStringContainsFilter(string queryValue, string dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, string, string>(x => x.Name);

        // Act
        var result = builder.HasStringContains();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Name = dataValue }));
    }

    [Theory]
    [InlineData("test", "test string", true)]
    [InlineData("test", "string test", false)]
    public void HasStringStartsWith_ShouldAddStringStartsWithFilter(string queryValue, string dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, string, string>(x => x.Name);

        // Act
        var result = builder.HasStringStartsWith();

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Name = dataValue }));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void HasCustomFilter_ShouldAddCustomFilter(int queryValue, int dataValue, bool expected)
    {
        // Arrange
        var builder = new ComplexFilterQueryPropertyBuilder<TestData, int, int>(x => x.Id);
        Expression<Func<int, int, bool>> customFilter = (queryProperty, dataProperty) => queryProperty == dataProperty;

        // Act
        var result = builder.HasCustomFilter(customFilter);

        // Assert
        Assert.NotNull(result);
        var filter = result.BuildFilterExpression(queryValue);
        var compiledFilter = filter.Compile();
        Assert.Equal(expected, compiledFilter(new TestData { Id = dataValue }));
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
