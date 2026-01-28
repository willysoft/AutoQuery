using AutoQuery.Abstractions;
using AutoQuery.Extensions;

namespace AutoQuery.Tests;

public class ApplyQueryCursorPagedResultTests
{
    [Fact]
    public void ApplyQueryCursorPagedResult_WithoutPageToken_ShouldGenerateNextPageToken()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" },
            new TestEntity { Id = 4, Name = "Item 4" },
            new TestEntity { Id = 5, Name = "Item 5" }
        };
        var query = data.AsQueryable();
        var queryProcessor = new QueryProcessor();
        
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Act - First request WITHOUT pageToken
        var queryOptions = new TestCursorQueryOptions { PageSize = 2, Sort = "Id" };
        var result = query.ApplyQueryCursorPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.NotNull(result.NextPageToken); // Should generate token even on first request!
        Assert.Null(result.PreviousPageToken); // First page, no previous
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_WithPageToken_ShouldNavigateToNextPage()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" },
            new TestEntity { Id = 4, Name = "Item 4" }
        };
        var query = data.AsQueryable();
        var queryProcessor = new QueryProcessor();
        
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Get first page
        var firstPageOptions = new TestCursorQueryOptions { PageSize = 2, Sort = "Id" };
        var firstPage = query.ApplyQueryCursorPagedResult(queryProcessor, firstPageOptions);

        // Act - Use token to get next page
        var nextPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2, 
            Sort = "Id",
            PageToken = firstPage.NextPageToken 
        };
        var nextPage = query.ApplyQueryCursorPagedResult(queryProcessor, nextPageOptions);

        // Assert
        Assert.Equal(2, nextPage.Datas.Count());
        Assert.Equal(3, nextPage.Datas.First().Id); // Should start from id 3
        Assert.NotNull(nextPage.PreviousPageToken); // Can navigate back
        Assert.Null(nextPage.NextPageToken); // Last page
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_UsingPreviousToken_ShouldNavigateBackward()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" },
            new TestEntity { Id = 4, Name = "Item 4" }
        };
        var query = data.AsQueryable();
        var queryProcessor = new QueryProcessor();
        
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Get first page
        var firstPageOptions = new TestCursorQueryOptions { PageSize = 2, Sort = "Id" };
        var firstPage = query.ApplyQueryCursorPagedResult(queryProcessor, firstPageOptions);

        // Get second page
        var secondPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2, 
            Sort = "Id",
            PageToken = firstPage.NextPageToken 
        };
        var secondPage = query.ApplyQueryCursorPagedResult(queryProcessor, secondPageOptions);

        // Act - Navigate back using previousPageToken
        var backPageOptions = new TestCursorQueryOptions 
        { 
            PageSize = 2, 
            Sort = "Id",
            PageToken = secondPage.PreviousPageToken 
        };
        var backPage = query.ApplyQueryCursorPagedResult(queryProcessor, backPageOptions);

        // Assert
        Assert.Equal(2, backPage.Datas.Count());
        Assert.Equal(1, backPage.Datas.First().Id); // Back to first page
        Assert.NotNull(backPage.NextPageToken); // Can navigate forward
        Assert.Null(backPage.PreviousPageToken); // First page
    }

    [Fact]
    public void ApplyQueryCursorPagedResult_WithFilters_ShouldApplyFiltersAndPagination()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Apple" },
            new TestEntity { Id = 2, Name = "Banana" },
            new TestEntity { Id = 3, Name = "Cherry" },
            new TestEntity { Id = 4, Name = "Date" },
            new TestEntity { Id = 5, Name = "Elderberry" }
        };
        var query = data.AsQueryable();
        var queryProcessor = new QueryProcessor();
        
        var builder = new FilterQueryBuilder<TestCursorQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasEqual()
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Act - Filter by id and paginate
        var queryOptions = new TestCursorQueryOptions 
        { 
            FilterId = 1,
            PageSize = 1,
            Sort = "Id"
        };
        var result = query.ApplyQueryCursorPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Single(result.Datas);
        Assert.Equal(1, result.Datas.First().Id);
        Assert.Null(result.NextPageToken); // Only one result matching filter
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestCursorQueryOptions : IQueryCursorPagedOptions
    {
        public int? FilterId { get; set; }
        public string? FilterName { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }
}
