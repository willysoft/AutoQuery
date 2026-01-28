using AutoQuery.Abstractions;
using AutoQuery.Extensions;

namespace AutoQuery.Tests;

public class HasCursorKeyTests
{
    [Fact]
    public void HasCursorKey_ShouldSetCursorKeySelector()
    {
        // Arrange
        var builder = new FilterQueryBuilder<TestQueryOptions, TestEntity>();

        // Act
        builder.Property(q => q.FilterKey, d => d.Key)
            .HasCursorKey();

        // Assert
        Assert.NotNull(builder.CursorKeySelector);
    }

    [Fact]
    public void HasCursorKey_ShouldUseCursorKeyForPagination()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new TestEntity { Key = "A001", Name = "Item 1" },
            new TestEntity { Key = "A002", Name = "Item 2" },
            new TestEntity { Key = "A003", Name = "Item 3" },
            new TestEntity { Key = "A004", Name = "Item 4" },
            new TestEntity { Key = "A005", Name = "Item 5" }
        };
        var query = data.AsQueryable();

        var builder = new FilterQueryBuilder<TestQueryOptions, TestEntity>();
        builder.Property(q => q.FilterKey, d => d.Key)
            .HasCursorKey();

        // Get first page without token
        var queryOptions = new TestQueryOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(queryOptions, builder.CursorKeySelector);

        // Act - Get second page using token
        var secondPageOptions = new TestQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var result = query.ApplyCursorBasedPaging(secondPageOptions, builder.CursorKeySelector);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal("A003", result.Datas.First().Key);
        Assert.Equal("A004", result.Datas.Last().Key);
    }

    [Fact]
    public void HasCursorKey_WithNumericProperty_ShouldWork()
    {
        // Arrange
        var data = new List<TestEntityNumericKey>
        {
            new TestEntityNumericKey { OrderId = 100, Name = "Order 1" },
            new TestEntityNumericKey { OrderId = 200, Name = "Order 2" },
            new TestEntityNumericKey { OrderId = 300, Name = "Order 3" },
            new TestEntityNumericKey { OrderId = 400, Name = "Order 4" }
        };
        var query = data.AsQueryable();

        var builder = new FilterQueryBuilder<TestNumericKeyQueryOptions, TestEntityNumericKey>();
        builder.Property(q => q.FilterOrderId, d => d.OrderId)
            .HasCursorKey();

        // Get first page
        var firstPageOptions = new TestNumericKeyQueryOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions, builder.CursorKeySelector);

        // Act - Get second page using token
        var secondPageOptions = new TestNumericKeyQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var result = query.ApplyCursorBasedPaging(secondPageOptions, builder.CursorKeySelector);

        // Assert
        Assert.Equal(2, result.Datas.Count());
        Assert.Equal(300, result.Datas.First().OrderId);
    }

    [Fact]
    public void HasCursorKey_WithMultipleProperties_ShouldUseLastOne()
    {
        // Arrange
        var builder = new FilterQueryBuilder<TestQueryOptions, TestEntity>();

        builder.Property(q => q.FilterKey, d => d.Key)
            .HasCursorKey();

        builder.Property(q => q.FilterName, d => d.Name)
            .HasCursorKey(); // This should override the previous one

        // Assert - The cursor key selector should be for Name property
        Assert.NotNull(builder.CursorKeySelector);
        
        // Create a test entity
        var entity = new TestEntity { Key = "K001", Name = "TestName" };
        var compiledSelector = builder.CursorKeySelector!.Compile();
        var result = compiledSelector(entity);
        
        // Should return the Name value (last configured cursor key)
        Assert.Equal("TestName", result);
    }

    [Fact]
    public void CursorBasedPagination_WithoutCursorKey_ShouldDefaultToId()
    {
        // Arrange
        var data = new List<TestEntityWithId>
        {
            new TestEntityWithId { Id = 1, Name = "Item 1" },
            new TestEntityWithId { Id = 2, Name = "Item 2" },
            new TestEntityWithId { Id = 3, Name = "Item 3" }
        };
        var query = data.AsQueryable();

        // Act - Without cursor key selector, should default to Id
        var firstPageOptions = new TestQueryOptions { PageSize = 2 };
        var firstPage = query.ApplyCursorBasedPaging(firstPageOptions, null);

        var secondPageOptions = new TestQueryOptions 
        { 
            PageSize = 2, 
            PageToken = firstPage.NextPageToken 
        };
        var result = query.ApplyCursorBasedPaging(secondPageOptions, null);

        // Assert - Should use Id for cursor
        Assert.Single(result.Datas);
        Assert.Equal(3, result.Datas.First().Id);
    }

    public class TestEntity
    {
        public string Key { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class TestEntityWithId
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestEntityNumericKey
    {
        public int OrderId { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TestQueryOptions : IQueryPagedOptions
    {
        public string? FilterKey { get; set; }
        public string? FilterName { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }

    public class TestNumericKeyQueryOptions : IQueryPagedOptions
    {
        public int? FilterOrderId { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? PageToken { get; set; }
    }
}
