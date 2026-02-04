using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks for concurrent query execution performance
/// Tests thread safety and scalability under parallel load
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ConcurrencyBenchmark
{
    private List<TestData> _testData = null!;
    private QueryProcessor _queryProcessor = null!;

    [Params(1, 4, 8, 16)]
    public int ThreadCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _queryProcessor = new QueryProcessor();
        _queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        _testData = Enumerable.Range(1, 10000)
                             .Select(i => new TestData { Id = i, Name = $"Name{i}", Age = i % 100, Score = i % 50 })
                             .ToList();
    }

    [Benchmark]
    public void ParallelQueries_DifferentFilters()
    {
        Parallel.For(0, ThreadCount, i =>
        {
            var queryOptions = new TestQueryOptions
            {
                Name = $"Name{i * 100}",
                Sort = "Age",
                Page = 1,
                PageSize = 10
            };

            _testData.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
        });
    }

    [Benchmark]
    public void ParallelQueries_SameFilter()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            var queryOptions = new TestQueryOptions
            {
                Name = "Name5000",
                Sort = "Age",
                Fields = "Id,Name",
                Page = 1,
                PageSize = 10
            };

            _testData.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
        });
    }

    [Benchmark]
    public void ParallelQueries_ComplexOperations()
    {
        Parallel.For(0, ThreadCount, i =>
        {
            var queryOptions = new TestQueryOptions
            {
                MinScore = i % 25,
                Sort = i % 2 == 0 ? "Age" : "-Age",
                Fields = "Id,Name,Score",
                Page = 1,
                PageSize = 20
            };

            _testData.AsQueryable().ApplyQueryPagedResult(_queryProcessor, queryOptions);
        });
    }

    public class UserQueryConfiguration : IFilterQueryConfiguration<TestQueryOptions, TestData>
    {
        public void Configure(FilterQueryBuilder<TestQueryOptions, TestData> builder)
        {
            builder.Property(q => q.Name, d => d.Name)
                   .HasEqual();
            builder.Property(q => q.MinScore, d => d.Score)
                   .HasGreaterThanOrEqual();
        }
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public int Score { get; set; }
    }

    public class TestQueryOptions : IQueryPagedOptions
    {
        public string? Name { get; set; }
        public int? MinScore { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
