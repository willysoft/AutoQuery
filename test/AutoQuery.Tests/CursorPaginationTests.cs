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

    [Fact]
    public void ApplyQueryCursorPaged_ShouldWorkWithCustomSorting()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            Sort = "id"
        };

        // Act
        var firstPage = _testData.ApplyQueryCursorPaged(_queryProcessor, queryOptions);
        var secondPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            Sort = "id",
            PageToken = firstPage.NextPageToken
        };
        var secondPage = _testData.ApplyQueryCursorPaged(_queryProcessor, secondPageOptions);

        // Assert
        Assert.Equal(2, firstPage.Count);
        Assert.Equal(1, firstPage.Datas.First().Id);
        Assert.Equal(2, secondPage.Count);
        Assert.Equal(3, secondPage.Datas.First().Id);
    }

    [Fact]
    public void PageToken_ShouldEncodeDecodeGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var token = PageToken.Encode(guid);
        var decoded = PageToken.Decode<Guid>(token);

        // Assert
        Assert.Equal(guid, decoded);
    }

    [Fact]
    public void PageToken_ShouldEncodeDecodeDateTime()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var token = PageToken.Encode(dateTime);
        var decoded = PageToken.Decode<DateTime>(token);

        // Assert
        Assert.Equal(dateTime, decoded);
    }

    [Fact]
    public void PageToken_ShouldEncodeDecodeLong()
    {
        // Arrange
        var longValue = 123456789L;

        // Act
        var token = PageToken.Encode(longValue);
        var decoded = PageToken.Decode<long>(token);

        // Assert
        Assert.Equal(longValue, decoded);
    }

    [Fact]
    public void ApplyQueryCursorPaged_WithLongCursorKey()
    {
        // Arrange
        var queryProcessor = new QueryProcessor();
        var builder = new FilterQueryBuilder<TestCursorQueryOptionsLong, TestDataLong>();
        builder.HasCursorKey(d => d.Id);
        queryProcessor.AddFilterQueryBuilder(builder);

        var testData = new List<TestDataLong>
        {
            new TestDataLong { Id = 1L, Name = "Item 1" },
            new TestDataLong { Id = 2L, Name = "Item 2" },
            new TestDataLong { Id = 3L, Name = "Item 3" },
        }.AsQueryable();

        var queryOptions = new TestCursorQueryOptionsLong { PageSize = 2 };

        // Act
        var result = testData.ApplyQueryCursorPaged(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result.NextPageToken);
    }

    [Fact]
    public void ApplyQueryCursorPaged_WithStringCursorKey()
    {
        // Arrange
        var queryProcessor = new QueryProcessor();
        var builder = new FilterQueryBuilder<TestCursorQueryOptionsString, TestDataString>();
        builder.HasCursorKey(d => d.Code);
        queryProcessor.AddFilterQueryBuilder(builder);

        var testData = new List<TestDataString>
        {
            new TestDataString { Code = "A001", Name = "Item 1" },
            new TestDataString { Code = "A002", Name = "Item 2" },
            new TestDataString { Code = "A003", Name = "Item 3" },
        }.AsQueryable();

        var queryOptions = new TestCursorQueryOptionsString { PageSize = 2, Sort = "code" };

        // Act
        var result = testData.ApplyQueryCursorPaged(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result.NextPageToken);
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestDataLong
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestDataString
    {
        public string Code { get; set; } = null!;
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

public class TestCursorQueryOptionsLong : IQueryCursorOptions
{
    public string? Fields { get; set; }
    public string? Sort { get; set; }
    public string? PageToken { get; set; }
    public int? PageSize { get; set; }
}

public class TestCursorQueryOptionsString : IQueryCursorOptions
{
    public string? Fields { get; set; }
    public string? Sort { get; set; }
    public string? PageToken { get; set; }
    public int? PageSize { get; set; }
}
