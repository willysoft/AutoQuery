using AutoQuery.Abstractions;
using AutoQuery.Extensions;

namespace AutoQuery.Tests;

public class CursorBasedPaginationTests
{
    [Fact]
    public void ApplyCursorBasedPaging_WithoutToken_ShouldReturnFirstPage()
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
        var queryOptions = new TestQueryPagedOptions { PageSize = 2 };

        // Act
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(1, result.Datas.First().Id);
        Assert.NotNull(result.NextPageToken);
    }

    [Fact]
    public void ApplyCursorBasedPaging_WithToken_ShouldReturnNextPage()
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
        
        // Get first page
        var firstPageOptions = new TestQueryPagedOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions);
        
        // Use token to get second page
        var secondPageOptions = new TestQueryPagedOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };

        // Act
        var result = query.ApplyCursorBasedPaging(secondPageOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(3, result.Datas.First().Id);
        Assert.NotNull(result.NextPageToken);
    }

    [Fact]
    public void ApplyCursorBasedPaging_LastPage_ShouldHaveNullNextToken()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" }
        };
        var query = data.AsQueryable();
        
        // Get first page (2 items)
        var firstPageOptions = new TestQueryPagedOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions);
        
        // Get last page using token
        var lastPageOptions = new TestQueryPagedOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };

        // Act
        var result = query.ApplyCursorBasedPaging(lastPageOptions);

        // Assert
        Assert.Equal(1, result.Datas.Count());
        Assert.Equal(3, result.Datas.First().Id);
        Assert.Null(result.NextPageToken); // No more pages
    }

    [Fact]
    public void ApplyCursorBasedPaging_WithInvalidToken_ShouldStartFromBeginning()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" }
        };
        var query = data.AsQueryable();
        var queryOptions = new TestQueryPagedOptions 
        { 
            PageSize = 2, 
            PageToken = "invalid-token-xyz" 
        };

        // Act
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(1, result.Datas.First().Id);
    }

    [Fact]
    public void ApplyCursorBasedPaging_EmptyResult_ShouldReturnEmptyWithoutToken()
    {
        // Arrange
        var data = new List<TestEntity>();
        var query = data.AsQueryable();
        var queryOptions = new TestQueryPagedOptions { PageSize = 2 };

        // Act
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Empty(result.Datas);
        Assert.Null(result.NextPageToken);
    }

    [Fact]
    public void ApplyCursorBasedPaging_DefaultPageSize_ShouldUse10()
    {
        // Arrange
        var data = Enumerable.Range(1, 15)
            .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
            .ToList();
        var query = data.AsQueryable();
        var queryOptions = new TestQueryPagedOptions(); // No PageSize specified

        // Act
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Equal(10, result.Datas.Count()); // Default is 10
        Assert.NotNull(result.NextPageToken);
    }

    [Fact]
    public void ApplyCursorBasedPaging_ShouldNotCalculateTotalCount()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" }
        };
        var query = data.AsQueryable();
        var queryOptions = new TestQueryPagedOptions { PageSize = 1 };

        // Act
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Equal(0, result.Count); // Should be 0 as we don't calculate total
        Assert.Equal(0, result.Page);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ApplyCursorBasedPaging_WithLargePageSize_ShouldCapAt1000()
    {
        // Arrange
        var data = Enumerable.Range(1, 2000)
            .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
            .ToList();
        var query = data.AsQueryable();
        var queryOptions = new TestQueryPagedOptions { PageSize = 5000 }; // Request very large page

        // Act
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Equal(1000, result.Datas.Count()); // Should be capped at 1000
    }

    [Fact]
    public void ApplyCursorBasedPaging_WithNullableId_ShouldWork()
    {
        // Arrange
        var data = new List<TestEntityNullableId>
        {
            new TestEntityNullableId { Id = 1, Name = "Item 1" },
            new TestEntityNullableId { Id = 2, Name = "Item 2" },
            new TestEntityNullableId { Id = 3, Name = "Item 3" },
            new TestEntityNullableId { Id = 4, Name = "Item 4" }
        };
        var query = data.AsQueryable();
        
        // Get first page
        var firstPageOptions = new TestQueryPagedOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions);
        
        // Use token to get second page
        var secondPageOptions = new TestQueryPagedOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };

        // Act
        var result = query.ApplyCursorBasedPaging(secondPageOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(3, result.Datas.First().Id);
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestEntityNullableId
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestQueryPagedOptions : IQueryPagedOptions
    {
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }
}
