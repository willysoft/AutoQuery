using AutoQuery.Extensions;
using System.Linq.Expressions;

namespace AutoQuery.Tests.Extensions;

public class SimpleFilterQueryPropertyBuilderExtensionsTests
{
    [Theory]
    [InlineData("test", "this is a test", true)]
    [InlineData("test", "no match", false)]
    public void HasStringContains_ShouldAddStringContainsFilter(string queryValue, string dataValue, bool expected)
    {
        // Arrange
        var builder = new SimpleFilterQueryPropertyBuilder<TestData, string>();

        // Act
        var result = builder.HasStringContains(d => d.Name);

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
        var builder = new SimpleFilterQueryPropertyBuilder<TestData, string>();

        // Act
        var result = builder.HasStringStartsWith(d => d.Name);

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
        var builder = new SimpleFilterQueryPropertyBuilder<TestData, int>();
        Expression<Func<int, TestData, bool>> customFilter = (queryProperty, data) => queryProperty == data.Id;

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
