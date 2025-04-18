using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class QueryPerformance
{
    private List<TestData> _autoQueryTestData = null!;
    private List<TestData> _dynamicLinqTestData = null!;
    private QueryProcessor _queryProcessor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _queryProcessor = new QueryProcessor();
        _queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        _autoQueryTestData = Enumerable.Range(1, 10000)
            .Select(i => new TestData { Id = i, Name = $"Name{i}", Age = i % 100 })
            .ToList();
        _dynamicLinqTestData = Enumerable.Range(1, 10000)
            .Select(i => new TestData { Id = i, Name = $"Name{i}", Age = i % 100 })
            .ToList();
    }

    [Benchmark]
    public void FilterSortSelectPageWithAutoQuery()
    {
        var queryOptions = new TestQueryOptions
        {
            Name = "Name5000",
            Sort = "Age",
            Fields = "Id,Name",
            Page = 1,
            PageSize = 10
        };

        _autoQueryTestData.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions);
    }

    [Benchmark]
    public void FilterSortSelectPageWithDynamicLinq()
    {
        _dynamicLinqTestData.AsQueryable()
                 .Where("Name == @0", "Name5000")
                 .OrderBy("Age")
                 .Select("new (Id, Name)")
                 .Skip(0)
                 .Take(10)
                 .ToDynamicList();
    }

    public class UserQueryConfiguration : IFilterQueryConfiguration<TestQueryOptions, TestData>
    {
        public void Configure(FilterQueryBuilder<TestQueryOptions, TestData> builder)
        {
            builder.Property(q => q.Name, d => d.Name)
                   .HasEqual();
        }
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    public class TestQueryOptions : IQueryPagedOptions
    {
        public string? Name { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
