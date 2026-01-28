using AutoQuery.Abstractions;
using AutoQuery.Extensions;

namespace AutoQuery.Tests;

public class CursorPaginationTests
{
    private readonly QueryProcessor _queryProcessor;
    private readonly IQueryable<TestData> _testData;

    public CursorPaginationTests()
    {
        _queryProcessor = new QueryProcessor();
        
        // Configure cursor-based pagination
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestData>();
        builder.HasCursorKey(d => d.Id);
        builder.Property(q => q.Name, d => d.Name).HasEqual();
        _queryProcessor.AddFilterQueryBuilder(builder);

        // Create test data
        _testData = new List<TestData>
        {
            new TestData { Id = 1, Name = "Item 1" },
            new TestData { Id = 2, Name = "Item 2" },
            new TestData { Id = 3, Name = "Item 3" },
            new TestData { Id = 4, Name = "Item 4" },
            new TestData { Id = 5, Name = "Item 5" },
        }.AsQueryable();
    }

    [Fact]
    public void HasCursorKey_ShouldSetCursorKeySelector()
    {
        // Arrange
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestData>();

        // Act
        builder.HasCursorKey(d => d.Id);
        var selector = builder.GetCursorKeySelector();

        // Assert
        Assert.NotNull(selector);
    }

    [Fact]
    public void GetCursorKeySelector_ShouldReturnNull_WhenNotConfigured()
    {
        // Arrange
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestData>();

        // Act
        var selector = builder.GetCursorKeySelector();

        // Assert
        Assert.Null(selector);
    }

    [Fact]
    public void ApplyQueryCursorPaged_ShouldReturnFirstPage_WhenNoTokenProvided()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions { PageSize = 2 };

        // Act
        var result = _testData.ApplyQueryCursorPaged(_queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result.NextPageToken);
        Assert.Equal(1, result.Datas.First().Id);
    }

    [Fact]
    public void ApplyQueryCursorPaged_ShouldReturnNextPage_WhenTokenProvided()
    {
        // Arrange
        var firstPageOptions = new TestCursorQueryOptions { PageSize = 2 };
        var firstPageResult = _testData.ApplyQueryCursorPaged(_queryProcessor, firstPageOptions);
        
        var secondPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            PageToken = firstPageResult.NextPageToken
        };

        // Act
        var result = _testData.ApplyQueryCursorPaged(_queryProcessor, secondPageOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result.Datas.First().Id);
    }

    [Fact]
    public void ApplyQueryCursorPaged_ShouldReturnNullNextToken_WhenNoMoreResults()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions { PageSize = 10 };

        // Act
        var result = _testData.ApplyQueryCursorPaged(_queryProcessor, queryOptions);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Null(result.NextPageToken);
    }

    [Fact]
    public void ApplyQueryCursorPaged_ShouldThrowException_WhenCursorKeyNotConfigured()
    {
        // Arrange
        var queryProcessor = new QueryProcessor();
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestData>();
        // Not configuring cursor key
        queryProcessor.AddFilterQueryBuilder(builder);
        
        var queryOptions = new TestCursorQueryOptions { PageSize = 2 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            _testData.ApplyQueryCursorPaged(queryProcessor, queryOptions));
    }

    [Fact]
    public void ApplyQueryCursorPaged_ShouldApplyFilters_WithCursorPagination()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            Name = "Item 3"
        };

        // Act
        var result = _testData.ApplyQueryCursorPaged(_queryProcessor, queryOptions);

        // Assert
        Assert.Single(result.Datas);
        Assert.Equal("Item 3", result.Datas.First().Name);
        Assert.Null(result.NextPageToken);
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}

public class TestCursorQueryOptions : IQueryCursorOptions
{
    public string? Name { get; set; }
    public string? Fields { get; set; }
    public string? Sort { get; set; }
    public string? PageToken { get; set; }
    public int? PageSize { get; set; }
}
