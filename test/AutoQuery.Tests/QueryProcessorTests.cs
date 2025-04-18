using AutoQuery.Abstractions;
using System.Reflection;

namespace AutoQuery.Tests;

public class QueryProcessorTests
{
    private readonly QueryProcessor _queryProcessor;

    public QueryProcessorTests()
    {
        _queryProcessor = new QueryProcessor();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildFilterExpression_ShouldReturnNull_WhenNoFilterQueryBuilder(string name)
    {
        // Arrange
        var queryOptions = new TestQueryOptions { Name = name };

        // Act
        var result = _queryProcessor.BuildFilterExpression<TestData, TestQueryOptions>(queryOptions);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildSelectorExpression_ShouldReturnNull_WhenFieldsAreEmpty(string fields)
    {
        // Arrange
        var queryOptions = new TestQueryOptions { Fields = fields };

        // Act
        var result = _queryProcessor.BuildSelectorExpression<TestData, TestQueryOptions>(queryOptions);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Name, Age", "John", 30, "123 Street")]
    public void BuildSelectorExpression_ShouldReturnExpression_WhenFieldsAreProvided(string fields, string name, int age, string address)
    {
        // Arrange
        var queryOptions = new TestQueryOptions { Fields = fields };

        // Act
        var result = _queryProcessor.BuildSelectorExpression<TestData, TestQueryOptions>(queryOptions);

        // Assert
        Assert.NotNull(result);
        var compiled = result.Compile();
        var testData = new TestData { Name = name, Age = age, Address = address };
        var selectedData = compiled(testData);
        Assert.Equal(name, selectedData.Name);
        Assert.Equal(age, selectedData.Age);
        Assert.Null(selectedData.Address);
    }

    [Theory]
    [InlineData("John", "John", true)]
    [InlineData("Jane", "John", false)]
    public void BuildFilterExpression_ShouldReturnExpression_WhenFilterQueryBuilderExists(string queryName, string dataName, bool expected)
    {
        // Arrange
        var queryOptions = new TestQueryOptions { Name = queryName };
        var filterQueryBuilder = new FilterQueryBuilder<TestQueryOptions, TestData>();
        filterQueryBuilder.Property(q => q.Name, d => d.Name)
                          .AddFilterExpression((q, d) => q == d, LogicalOperator.AND);
        _queryProcessor.AddFilterQueryBuilder(filterQueryBuilder);

        // Act
        var result = _queryProcessor.BuildFilterExpression<TestData, TestQueryOptions>(queryOptions);

        // Assert
        Assert.NotNull(result);
        var compiled = result.Compile();
        var testData = new TestData { Name = dataName, Age = 30, Address = "123 Street" };
        var isMatch = compiled(testData);
        Assert.Equal(expected, isMatch);
    }

    [Fact]
    public void AddFilterQueryBuilder_ShouldAddBuilder()
    {
        // Arrange
        var filterQueryBuilder = new FilterQueryBuilder<TestQueryOptions, TestData>();

        // Act
        _queryProcessor.AddFilterQueryBuilder(filterQueryBuilder);

        // Assert
        Assert.Single(_queryProcessor._builders);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_ShouldAddBuilders()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        _queryProcessor.ApplyConfigurationsFromAssembly(assembly);

        // Assert
        Assert.Single(_queryProcessor._builders);
    }

    public class TestQueryOptions : IQueryOptions
    {
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public string? SortDirection { get; set; }
        public string? Name { get; set; }
    }

    public class TestData
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Address { get; set; } = null!;
    }

    public class UserQueryConfiguration : IFilterQueryConfiguration<TestQueryOptions, TestData>
    {
        public void Configure(FilterQueryBuilder<TestQueryOptions, TestData> builder)
        {
            builder.Property(q => q.Name, u => u.Name)
                   .AddFilterExpression((q, u) => q != null && u.Contains(q), LogicalOperator.AND);
        }
    }
}
