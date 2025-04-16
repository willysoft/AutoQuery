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
    public void ApplyQueryPaged_ShouldApplyFilterSelectorAndPaging(List<TestData> data, TestQueryPagedOptions queryOptions, Expression<Func<TestData, bool>> filterExpression, Expression<Func<TestData, TestData>> selectorExpression, int expectedCount, int totalCount, int totalPages)
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
        Assert.Equal(expectedCount, result.Datas.Count());
        Assert.Equal(totalCount, result.Count);
        Assert.Equal(totalPages, result.TotalPages);
    }

    [Theory]
    [ClassData(typeof(ApplySortTestData))]
    public void ApplySort_ShouldSortData(List<TestData> data, TestQueryOptions queryOptions, string expectedFirstName)
    {
        // Arrange
        var queryableData = data.AsQueryable();

        // Act
        var result = queryableData.ApplySort(queryOptions);

        // Assert
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
                2,
                2
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
                1
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
                2
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
                new TestQueryOptions {Sort = "Name"},
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
                "F"
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
