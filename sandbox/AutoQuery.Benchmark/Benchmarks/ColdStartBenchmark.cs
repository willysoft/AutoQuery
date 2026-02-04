using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks for cold start performance - measures first query latency
/// Estimated impact: 50-70% reduction in first-query latency (Phase 1 goal)
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ColdStartBenchmark
{
    private List<TestData> _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testData = Enumerable.Range(1, 1000)
                             .Select(i => new TestData { Id = i, Name = $"Name{i}", Age = i % 100 })
                             .ToList();
    }

    [Benchmark]
    public void FirstQuery_WithConfiguration()
    {
        // Simulate cold start by creating new processor each time
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions
        {
            Name = "Name500",
            Sort = "Age",
            Fields = "Id,Name",
            Page = 1,
            PageSize = 10
        };

        _testData.AsQueryable().ApplyQueryPaged(queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void FirstQuery_SimpleFilter()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions
        {
            Name = "Name500"
        };

        _testData.AsQueryable().ApplyQuery(queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void FirstQuery_ComplexOperation()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions
        {
            Name = "Name500",
            Sort = "-Age",
            Fields = "Id,Name,Age",
            Page = 1,
            PageSize = 20
        };

        _testData.AsQueryable().ApplyQueryPagedResult(queryProcessor, queryOptions);
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
