using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks for large dataset performance (100K+, 1M+ records)
/// Tests scalability with increasing data volumes
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class LargeDatasetBenchmark
{
    private List<TestData> _data100K = null!;
    private List<TestData> _data500K = null!;
    private List<TestData> _data1M = null!;
    private QueryProcessor _queryProcessor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _queryProcessor = new QueryProcessor();
        _queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // 100K records
        _data100K = Enumerable.Range(1, 100_000)
                             .Select(i => new TestData 
                             { 
                                 Id = i, 
                                 Name = $"Name{i}", 
                                 Age = i % 100,
                                 Category = $"Cat{i % 10}",
                                 Score = i % 1000
                             })
                             .ToList();

        // 500K records
        _data500K = Enumerable.Range(1, 500_000)
                             .Select(i => new TestData 
                             { 
                                 Id = i, 
                                 Name = $"Name{i}", 
                                 Age = i % 100,
                                 Category = $"Cat{i % 10}",
                                 Score = i % 1000
                             })
                             .ToList();

        // 1M records
        _data1M = Enumerable.Range(1, 1_000_000)
                           .Select(i => new TestData 
                           { 
                               Id = i, 
                               Name = $"Name{i}", 
                               Age = i % 100,
                               Category = $"Cat{i % 10}",
                               Score = i % 1000
                           })
                           .ToList();
    }

    [Benchmark]
    public void Query_100K_FilterSortPage()
    {
        var queryOptions = new TestQueryOptions
        {
            Category = "Cat5",
            Sort = "-Score",
            Page = 1,
            PageSize = 100
        };

        _data100K.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_100K_ComplexFilter()
    {
        var queryOptions = new TestQueryOptions
        {
            Category = "Cat5",
            MinAge = 25,
            MaxAge = 75,
            MinScore = 500,
            Sort = "-Score,Name",
            Fields = "Id,Name,Score",
            Page = 1,
            PageSize = 100
        };

        _data100K.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_500K_FilterSortPage()
    {
        var queryOptions = new TestQueryOptions
        {
            Category = "Cat5",
            Sort = "-Score",
            Page = 1,
            PageSize = 100
        };

        _data500K.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_1M_FilterSortPage()
    {
        var queryOptions = new TestQueryOptions
        {
            Category = "Cat5",
            Sort = "-Score",
            Page = 1,
            PageSize = 100
        };

        _data1M.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_1M_FullScan()
    {
        var queryOptions = new TestQueryOptions
        {
            MinScore = 999,
            Sort = "-Score"
        };

        _data1M.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void Query_1M_PagedResult()
    {
        var queryOptions = new TestQueryOptions
        {
            Category = "Cat5",
            MinAge = 50,
            Sort = "-Score,Age",
            Fields = "Id,Name,Age,Score",
            Page = 10,
            PageSize = 100
        };

        _data1M.AsQueryable().ApplyQueryPagedResult(_queryProcessor, queryOptions);
    }

    public class UserQueryConfiguration : IFilterQueryConfiguration<TestQueryOptions, TestData>
    {
        public void Configure(FilterQueryBuilder<TestQueryOptions, TestData> builder)
        {
            builder.Property(q => q.Category, d => d.Category)
                   .HasEqual();
            builder.Property(q => q.MinAge, d => d.Age)
                   .HasGreaterThanOrEqual();
            builder.Property(q => q.MaxAge, d => d.Age)
                   .HasLessThanOrEqual();
            builder.Property(q => q.MinScore, d => d.Score)
                   .HasGreaterThanOrEqual();
        }
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string Category { get; set; } = null!;
        public int Score { get; set; }
    }

    public class TestQueryOptions : IQueryPagedOptions
    {
        public string? Category { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? MinScore { get; set; }
        public string? Fields { get; set; }
        public string? Sort { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
