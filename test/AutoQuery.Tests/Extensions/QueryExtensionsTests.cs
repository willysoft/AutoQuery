using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using Moq;
using System.Collections;
using System.Linq.Expressions;

namespace AutoQuery.Tests.Extensions;

public class QueryExtensionsTests
{
    private readonly Mock<IQueryProcessor> _queryProcessorMock;

    public QueryExtensionsTests()
    {
        _queryProcessorMock = new Mock<IQueryProcessor>();
    }

    [Theory]
    [ClassData(typeof(ApplyQueryTestData))]
    public void ApplyQuery_ShouldApplyFilterAndSelector(List<TestData> data, TestQueryOptions queryOptions, Expression<Func<TestData, bool>> filterExpression, Expression<Func<TestData, TestData>> selectorExpression, int expectedCount, string expectedName)
    {
        // Arrange
        var queryableData = data.AsQueryable();

        _queryProcessorMock.Setup(x => x.BuildFilterExpression<TestData, TestQueryOptions>(queryOptions))
                           .Returns(filterExpression);
        _queryProcessorMock.Setup(x => x.BuildSelectorExpression<TestData, TestQueryOptions>(queryOptions))
                           .Returns(selectorExpression);

        // Act
        var result = queryableData.ApplyQuery(_queryProcessorMock.Object, queryOptions);

        // Assert
        Assert.Equal(expectedCount, result.Count());
        Assert.Equal(expectedName, result.First().Name);
    }

    [Theory]
    [ClassData(typeof(ApplyQueryPagedTestData))]
    public void ApplyQueryPaged_ShouldApplyFilterSelectorAndPaging(List<TestData> data, TestQueryPagedOptions queryOptions, Expression<Func<TestData, bool>> filterExpression, Expression<Func<TestData, TestData>> selectorExpression, int expectedCount, string expectedName)
    {
        // Arrange
        var queryableData = data.AsQueryable();

        _queryProcessorMock.Setup(x => x.BuildFilterExpression<TestData, TestQueryPagedOptions>(queryOptions))
                           .Returns(filterExpression);
        _queryProcessorMock.Setup(x => x.BuildSelectorExpression<TestData, TestQueryPagedOptions>(queryOptions))
                           .Returns(selectorExpression);

        // Act
        var result = queryableData.ApplyQueryPaged(_queryProcessorMock.Object, queryOptions);

        // Assert
        Assert.Equal(expectedCount, result.Count());
        Assert.Equal(expectedName, result.First().Name);
    }

    [Theory]
    [ClassData(typeof(ApplyQueryPagedResultTestData))]
    public void ApplyQueryPagedResult_ShouldApplyFilterSelectorAndPaging(List<TestData> data, TestQueryPagedOptions queryOptions, Expression<Func<TestData, bool>> filterExpression, Expression<Func<TestData, TestData>> selectorExpression, int expectedCount, int totalCount, int totalPages, string expectedName)
    {
        // Arrange
        var queryableData = data.AsQueryable();

        _queryProcessorMock.Setup(x => x.BuildFilterExpression<TestData, TestQueryPagedOptions>(queryOptions))
                           .Returns(filterExpression);
        _queryProcessorMock.Setup(x => x.BuildSelectorExpression<TestData, TestQueryPagedOptions>(queryOptions))
                           .Returns(selectorExpression);

        // Act
        var result = queryableData.ApplyQueryPagedResult(_queryProcessorMock.Object, queryOptions);

        // Assert
        Assert.Equal(expectedCount, result.Datas.Count());
        Assert.Equal(totalCount, result.Count);
        Assert.Equal(totalPages, result.TotalPages);
        Assert.Equal(expectedName, result.Datas.First().Name);
    }

    [Theory]
    [ClassData(typeof(ApplySortTestData))]
    public void ApplySort_ShouldSortData(List<TestData> data, TestQueryOptions queryOptions, int expectedFirstId, string expectedFirstName)
    {
        // Arrange
        var queryableData = data.AsQueryable();

        // Act
        var result = queryableData.ApplySort(queryOptions);

        // Assert
        Assert.Equal(expectedFirstId, result.First().Id);
        Assert.Equal(expectedFirstName, result.First().Name);
    }

    [Theory]
    [ClassData(typeof(ApplyPagingTestData))]
    public void ApplyPaging_ShouldPageData(List<TestData> data, TestQueryPagedOptions queryOptions, int expectedCount, string expectedFirstName)
    {
        // Arrange
        var queryableData = data.AsQueryable();

        // Act
        var result = queryableData.ApplyPaging(queryOptions);

        // Assert
        Assert.Equal(expectedCount, result.Count());
        Assert.Equal(expectedFirstName, result.First().Name);
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestQueryOptions : IQueryOptions
    {
        public string? Fields { get; set; }
        public string? Sort { get; set; }
    }

    public class TestQueryPagedOptions : TestQueryOptions, IQueryPagedOptions
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public class ApplyQueryTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<TestData> { new TestData { Id = 1, Name = "Test" } },
                new TestQueryOptions(),
                (Expression<Func<TestData, bool>>)(x => x.Id == 1),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                "Test"
            };
            yield return new object[]
            {
                new List<TestData> { new TestData { Id = 2, Name = "Sample" } },
                new TestQueryOptions(),
                (Expression<Func<TestData, bool>>)(x => x.Id == 2),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                "Sample"
            };
            yield return new object[]
            {
                new List<TestData> { new TestData { Id = 3, Name = "Example" } },
                new TestQueryOptions(),
                (Expression<Func<TestData, bool>>)(x => x.Id == 3),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                "Example"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "Test1" },
                    new TestData { Id = 2, Name = "Test2" },
                    new TestData { Id = 3, Name = "Test3" }
                },
                new TestQueryOptions(),
                (Expression<Func<TestData, bool>>)(x => x.Id > 1),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                2,
                "Test2"
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ApplyQueryPagedTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "Test1" },
                    new TestData { Id = 2, Name = "Test2" }
                },
                new TestQueryPagedOptions { Page = 1, PageSize = 1 },
                (Expression<Func<TestData, bool>>)(x => x.Id > 0),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                "Test1"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 3, Name = "Test3" },
                    new TestData { Id = 4, Name = "Test4" }
                },
                new TestQueryPagedOptions { Page = 1, PageSize = 2 },
                (Expression<Func<TestData, bool>>)(x => x.Id > 2),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                2,
                "Test3"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 5, Name = "Test5" },
                    new TestData { Id = 6, Name = "Test6" },
                    new TestData { Id = 7, Name = "Test7" }
                },
                new TestQueryPagedOptions { Page = 2, PageSize = 2 },
                (Expression<Func<TestData, bool>>)(x => x.Id > 4),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                "Test7"
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ApplyQueryPagedResultTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "Test1" },
                    new TestData { Id = 2, Name = "Test2" }
                },
                new TestQueryPagedOptions { Page = 1, PageSize = 1 },
                (Expression<Func<TestData, bool>>)(x => x.Id > 0),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                2,
                2,
                "Test1"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 3, Name = "Test3" },
                    new TestData { Id = 4, Name = "Test4" }
                },
                new TestQueryPagedOptions { Page = 1, PageSize = 2 },
                (Expression<Func<TestData, bool>>)(x => x.Id > 2),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                2,
                2,
                1,
                "Test3"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 5, Name = "Test5" },
                    new TestData { Id = 6, Name = "Test6" },
                    new TestData { Id = 7, Name = "Test7" }
                },
                new TestQueryPagedOptions { Page = 2, PageSize = 2 },
                (Expression<Func<TestData, bool>>)(x => x.Id > 4),
                (Expression<Func<TestData, TestData>>)(x => new TestData { Id = x.Id, Name = x.Name }),
                1,
                3,
                2,
                "Test7"
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ApplySortTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "B" },
                    new TestData { Id = 2, Name = "A" }
                },
                new TestQueryOptions { Sort = "Name" },
                2,
                "A"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "B" },
                    new TestData { Id = 2, Name = "A" }
                },
                new TestQueryOptions { Sort = "-Name" },
                1,
                "B"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 3, Name = "C" },
                    new TestData { Id = 4, Name = "D" }
                },
                new TestQueryOptions { Sort = "Name" },
                3,
                "C"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 5, Name = "E" },
                    new TestData { Id = 6, Name = "F" }
                },
                new TestQueryOptions { Sort = "-Name" },
                6,
                "F"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 2, Name = "A" },
                    new TestData { Id = 1, Name = "A" },
                    new TestData { Id = 3, Name = "B" }
                },
                new TestQueryOptions { Sort = "Name,Id" },
                1,
                "A"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "A" },
                    new TestData { Id = 2, Name = "A" },
                    new TestData { Id = 3, Name = "B" }
                },
                new TestQueryOptions { Sort = "Name,-Id" },
                2,
                "A"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "C" },
                    new TestData { Id = 2, Name = "B" },
                    new TestData { Id = 2, Name = "A" }
                },
                new TestQueryOptions { Sort = "-Id,Name" },
                2,
                "A"
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ApplyPagingTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 1, Name = "Test1" },
                    new TestData { Id = 2, Name = "Test2" }
                },
                new TestQueryPagedOptions { Page = 2, PageSize = 1 },
                1,
                "Test2"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 3, Name = "Test3" },
                    new TestData { Id = 4, Name = "Test4" }
                },
                new TestQueryPagedOptions { Page = 1, PageSize = 2 },
                 2,
                "Test3"
            };
            yield return new object[]
            {
                new List<TestData>
                {
                    new TestData { Id = 5, Name = "Test5" },
                    new TestData { Id = 6, Name = "Test6" },
                    new TestData { Id = 7, Name = "Test7" }
                },
                new TestQueryPagedOptions { Page = 3, PageSize = 1 },
                1,
                "Test7"
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public class QueryExtensionsCursorPaginationTests
{
    private readonly QueryProcessor _queryProcessor;
    private readonly IQueryable<TestData> _testData;

    public QueryExtensionsCursorPaginationTests()
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
    public void ApplyQueryCursorPagedResult_ShouldReturnFirstPage_WhenNoTokenProvided()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions { PageSize = 2 };

        // Act
        var result = _testData.ApplyQueryCursorPagedResult(_queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result.NextPageToken);
        Assert.Equal(1, result.Datas.First().Id);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_ShouldReturnNextPage_WhenTokenProvided()
    {
        // Arrange
        var firstPageOptions = new TestCursorQueryOptions { PageSize = 2 };
        var firstPageResult = _testData.ApplyQueryCursorPagedResult(_queryProcessor, firstPageOptions);
        
        var secondPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            PageToken = firstPageResult.NextPageToken
        };

        // Act
        var result = _testData.ApplyQueryCursorPagedResult(_queryProcessor, secondPageOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result.Datas.First().Id);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_ShouldReturnNullNextToken_WhenNoMoreResults()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions { PageSize = 10 };

        // Act
        var result = _testData.ApplyQueryCursorPagedResult(_queryProcessor, queryOptions);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Null(result.NextPageToken);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_ShouldThrowException_WhenCursorKeyNotConfigured()
    {
        // Arrange
        var queryProcessor = new QueryProcessor();
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestData>();
        // Not configuring cursor key
        queryProcessor.AddFilterQueryBuilder(builder);
        
        var queryOptions = new TestCursorQueryOptions { PageSize = 2 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            _testData.ApplyQueryCursorPagedResult(queryProcessor, queryOptions));
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_ShouldApplyFilters_WithCursorPagination()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            Name = "Item 3"
        };

        // Act
        var result = _testData.ApplyQueryCursorPagedResult(_queryProcessor, queryOptions);

        // Assert
        Assert.Single(result.Datas);
        Assert.Equal("Item 3", result.Datas.First().Name);
        Assert.Null(result.NextPageToken);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_ShouldWorkWithCustomSorting()
    {
        // Arrange
        var queryOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            Sort = "id"
        };

        // Act
        var firstPage = _testData.ApplyQueryCursorPagedResult(_queryProcessor, queryOptions);
        var secondPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2,
            Sort = "id",
            PageToken = firstPage.NextPageToken
        };
        var secondPage = _testData.ApplyQueryCursorPagedResult(_queryProcessor, secondPageOptions);

        // Assert
        Assert.Equal(2, firstPage.Count);
        Assert.Equal(1, firstPage.Datas.First().Id);
        Assert.Equal(2, secondPage.Count);
        Assert.Equal(3, secondPage.Datas.First().Id);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_WithLongCursorKey()
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
        var result = testData.ApplyQueryCursorPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result.NextPageToken);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_WithStringCursorKey()
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
        var result = testData.ApplyQueryCursorPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result.NextPageToken);
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_WithDescendingSort()
    {
        // Arrange
        var queryProcessor = new QueryProcessor();
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestData>();
        builder.HasCursorKey(d => d.Id);
        queryProcessor.AddFilterQueryBuilder(builder);

        var testData = new List<TestData>
        {
            new TestData { Id = 1, Name = "Item 1" },
            new TestData { Id = 2, Name = "Item 2" },
            new TestData { Id = 3, Name = "Item 3" },
            new TestData { Id = 4, Name = "Item 4" },
            new TestData { Id = 5, Name = "Item 5" },
        }.AsQueryable();

        var firstPageOptions = new TestCursorQueryOptions { PageSize = 2, Sort = "-id" };

        // Act - First page
        var firstPage = testData.ApplyQueryCursorPagedResult(queryProcessor, firstPageOptions);

        // Assert - First page should have items 5 and 4 (descending order)
        Assert.Equal(2, firstPage.Count);
        Assert.Equal(5, firstPage.Datas.First().Id);
        Assert.Equal(4, firstPage.Datas.Last().Id);
        Assert.NotNull(firstPage.NextPageToken);

        // Act - Second page
        var secondPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2, 
            Sort = "-id", 
            PageToken = firstPage.NextPageToken 
        };
        var secondPage = testData.ApplyQueryCursorPagedResult(queryProcessor, secondPageOptions);

        // Assert - Second page should have items 3 and 2 (descending order)
        Assert.Equal(2, secondPage.Count);
        Assert.Equal(3, secondPage.Datas.First().Id);
        Assert.Equal(2, secondPage.Datas.Last().Id);
        Assert.NotNull(secondPage.NextPageToken);

        // Act - Third page
        var thirdPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2, 
            Sort = "-id", 
            PageToken = secondPage.NextPageToken 
        };
        var thirdPage = testData.ApplyQueryCursorPagedResult(queryProcessor, thirdPageOptions);

        // Assert - Third page should have item 1 only
        Assert.Equal(1, thirdPage.Count);
        Assert.Equal(1, thirdPage.Datas.First().Id);
        Assert.Null(thirdPage.NextPageToken);
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
}
