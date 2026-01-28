using AutoQuery.Abstractions;
using AutoQuery.Extensions;

namespace AutoQuery.Tests;

public class PreviousPageTokenTests
{
    [Fact]
    public void ApplyCursorBasedPaging_WithPageToken_ShouldGeneratePreviousPageToken()
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
        var firstPageOptions = new TestQueryOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions);
        
        // Act - Use token to get second page
        var secondPageOptions = new TestQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var result = query.ApplyCursorBasedPaging(secondPageOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(3, result.Datas.First().Id);
        Assert.NotNull(result.PreviousPageToken); // Should have previous token
        Assert.NotNull(result.NextPageToken); // Should also have next token
    }

    [Fact]
    public void ApplyCursorBasedPaging_FirstPage_ShouldNotHavePreviousPageToken()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item 1" },
            new TestEntity { Id = 2, Name = "Item 2" },
            new TestEntity { Id = 3, Name = "Item 3" }
        };
        var query = data.AsQueryable();

        // Act - First page without token
        var queryOptions = new TestQueryOptions { PageSize = 2 };
        var result = query.ApplyCursorBasedPaging(queryOptions);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Null(result.PreviousPageToken); // First page should not have previous token
        Assert.NotNull(result.NextPageToken); // But should have next token
    }

    [Fact]
    public void ApplyCursorBasedPaging_BackwardNavigation_ShouldWork()
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
        
        // Get first page
        var firstPageOptions = new TestQueryOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions);
        
        // Get second page
        var secondPageOptions = new TestQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var secondPage = query.ApplyCursorBasedPaging(secondPageOptions);

        // Act - Navigate back using previous token
        var backOptions = new TestQueryOptions 
        { 
            PageSize = 2, 
            PageToken = secondPage.PreviousPageToken 
        };
        var result = query.ApplyCursorBasedPaging(backOptions);

        // Assert - Should get items before Id 3 (the first item of second page)
        Assert.Equal(2, result.Datas.Count());
        // Items should be less than 3
        Assert.All(result.Datas, item => Assert.True(item.Id < 3));
    }

    [Fact]
    public void ApplyCursorBasedPaging_WithStringCursorKey_ShouldGeneratePreviousPageToken()
    {
        // Arrange
        var data = new List<TestEntityWithSKU>
        {
            new TestEntityWithSKU { SKU = "SKU-001", Name = "Item 1" },
            new TestEntityWithSKU { SKU = "SKU-002", Name = "Item 2" },
            new TestEntityWithSKU { SKU = "SKU-003", Name = "Item 3" },
            new TestEntityWithSKU { SKU = "SKU-004", Name = "Item 4" }
        };
        var query = data.AsQueryable().OrderBy(x => x.SKU);
        
        var queryProcessor = new QueryProcessor();
        var builder = new FilterQueryBuilder<TestSKUQueryOptions, TestEntityWithSKU>();
        builder.Property(q => q.FilterSKU, d => d.SKU)
            .HasCursorKey();
        queryProcessor.AddFilterQueryBuilder(builder);

        var cursorKeySelector = queryProcessor.GetCursorKeySelector<TestSKUQueryOptions, TestEntityWithSKU>();
        
        // Get first page
        var firstPageOptions = new TestSKUQueryOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions, cursorKeySelector);
        
        // Act - Get second page
        var secondPageOptions = new TestSKUQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var result = query.ApplyCursorBasedPaging(secondPageOptions, cursorKeySelector);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal("SKU-003", result.Datas.First().SKU);
        Assert.NotNull(result.PreviousPageToken); // Should have previous token
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestEntityWithSKU
    {
        public string SKU { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class TestQueryOptions : IQueryPagedOptions
    {
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }

    public class TestSKUQueryOptions : IQueryPagedOptions
    {
        public string? FilterSKU { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }
}