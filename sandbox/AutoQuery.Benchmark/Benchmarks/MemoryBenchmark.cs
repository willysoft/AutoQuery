using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks for memory performance and GC pressure
/// Estimated impact: 30-40% reduction in GC pressure (Phase 1 goal)
/// Measures allocations and Gen0/Gen1/Gen2 collections
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class MemoryBenchmark
{
    private List<TestData> _testData = null!;
    private QueryProcessor _queryProcessor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _queryProcessor = new QueryProcessor();
        _queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        _testData = Enumerable.Range(1, 10000)
                             .Select(i => new TestData 
                             { 
                                 Id = i, 
                                 Name = $"Name{i}", 
                                 Age = i % 100, 
                                 Email = $"user{i}@example.com",
                                 Description = $"Description for user {i}"
                             })
                             .ToList();
    }

    [Benchmark]
    public void Query_StringParsing_Fields()
    {
        // Tests memory allocation during Fields parsing
        var queryOptions = new TestQueryOptions
        {
            Fields = "Id,Name,Age,Email,Description"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_StringParsing_Sort()
    {
        // Tests memory allocation during Sort parsing
        var queryOptions = new TestQueryOptions
        {
            Sort = "-Age,Name,-Id"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_RepeatedFieldsParsing()
    {
        // Simulates repeated queries with same Fields to test caching
        for (int i = 0; i < 100; i++)
        {
            var queryOptions = new TestQueryOptions
            {
                Name = $"Name{i * 100}",
                Fields = "Id,Name,Age"
            };

            _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).Take(1).ToList();
        }
    }

    [Benchmark]
    public void Query_RepeatedSortParsing()
    {
        // Simulates repeated queries with same Sort to test caching
        for (int i = 0; i < 100; i++)
        {
            var queryOptions = new TestQueryOptions
            {
                Name = $"Name{i * 100}",
                Sort = "-Age,Name"
            };

            _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).Take(1).ToList();
        }
    }

    [Benchmark]
    public void Query_FullPipeline_WithAllocation()
    {
        // Tests full query pipeline memory allocation
        var results = new List<TestData>();
        for (int i = 0; i < 10; i++)
        {
            var queryOptions = new TestQueryOptions
            {
                Name = $"Name{i * 1000}",
                Fields = "Id,Name,Email",
                Sort = "-Age",
                Page = 1,
                PageSize = 100
            };

            var result = _testData.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
            results.AddRange(result);
        }
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
        public string Email { get; set; } = null!;
        public string Description { get; set; } = null!;
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
