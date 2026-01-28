using AutoQuery.Abstractions;
using AutoQuery.Extensions;

namespace AutoQuery.Tests;

public class OffsetPaginationPageTokenTests
{
    [Fact]
    public void ApplyQueryPagedResult_OffsetPagination_ShouldGenerateNextPageToken()
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
        
        var builder = new FilterQueryBuilder<TestQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Act - First page with offset pagination
        var queryOptions = new TestQueryOptions { Page = 1, PageSize = 2 };
        var result = query.ApplyQueryPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(5, result.Count);
        Assert.NotNull(result.NextPageToken); // Should have next page token
        Assert.Null(result.PreviousPageToken);
    }

    [Fact]
    public void ApplyQueryPagedResult_LastPage_ShouldNotGenerateNextPageToken()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" }
        };
        var query = data.AsQueryable();
        var queryProcessor = new QueryProcessor();
        
        var builder = new FilterQueryBuilder<TestQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Act - Last page
        var queryOptions = new TestQueryOptions { Page = 2, PageSize = 2 };
        var result = query.ApplyQueryPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Single(result.Datas);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.TotalPages);
        Assert.Null(result.NextPageToken); // Last page should not have token
    }

    [Fact]
    public void ApplyQueryPagedResult_GeneratedToken_CanBeUsedForCursorPagination()
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
        
        var builder = new FilterQueryBuilder<TestQueryOptions, TestEntity>();
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        // Get first page and token
        var firstPageOptions = new TestQueryOptions { Page = 1, PageSize = 2 };
        var firstPage = query.ApplyQueryPagedResult(queryProcessor, firstPageOptions);

        // Act - Use generated token for cursor-based pagination
        var cursorPageOptions = new TestQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var cursorPage = query.ApplyQueryPagedResult(queryProcessor, cursorPageOptions);

        // Assert
        Assert.Equal(2, cursorPage.Datas.Count());
        Assert.Equal(3, cursorPage.Datas.First().Id);
        Assert.Equal(0, cursorPage.Page); // Cursor mode doesn't use page number
        Assert.Equal(0, cursorPage.TotalPages); // Cursor mode doesn't calculate total
    }

    [Fact]
    public void ApplyQueryPagedResult_WithoutCursorKeyConfig_ShouldStillGenerateToken()
    {
        // Arrange - Entity with Id property but no cursor key configured
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" }
        };
        var query = data.AsQueryable();
        var queryProcessor = new QueryProcessor();
        
        // No cursor key configured - should default to "Id"

        // Act
        var queryOptions = new TestQueryOptions { Page = 1, PageSize = 2 };
        var result = query.ApplyQueryPagedResult(queryProcessor, queryOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.NotNull(result.NextPageToken); // Should still generate token using default Id
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestQueryOptions : IQueryPagedOptions
    {
        public int? FilterId { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }
}
