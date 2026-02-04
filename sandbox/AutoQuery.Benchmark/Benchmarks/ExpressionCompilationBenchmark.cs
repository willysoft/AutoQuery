using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks for expression tree compilation performance
/// Target for Phase 1 optimization: Implement warmup mechanism and Lazy<T> compilation
/// Estimated impact: 50-70% reduction in first-query latency
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class ExpressionCompilationBenchmark
{
    private List<TestData> _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testData = Enumerable.Range(1, 1000)
                             .Select(i => new TestData 
                             { 
                                 Id = i, 
                                 Name = $"Name{i}", 
                                 Age = i % 100,
                                 Score = i % 50
                             })
                             .ToList();
    }

    [Benchmark]
    public void CompileFilterExpression_FirstTime()
    {
        // Simulates cold start - new processor for each iteration
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions { Name = "Name500" };
        _testData.AsQueryable().ApplyQuery(queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void CompileSelectorExpression_FirstTime()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions { Fields = "Id,Name,Age" };
        _testData.AsQueryable().ApplyQuery(queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void CompileSortExpression_FirstTime()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions { Sort = "-Age,Name" };
        _testData.AsQueryable().ApplyQuery(queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void CompileComplexExpression_FirstTime()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var queryOptions = new TestQueryOptions 
        { 
            Name = "Name500",
            MinScore = 25,
            Fields = "Id,Name,Age,Score",
            Sort = "-Score,Age",
            Page = 1,
            PageSize = 10
        };

        _testData.AsQueryable().ApplyQueryPagedResult(queryProcessor, queryOptions);
    }

    [Benchmark]
    public void CompileMultipleExpressions_Sequential()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // First query - triggers compilation
        var options1 = new TestQueryOptions { Name = "Name100" };
        _testData.AsQueryable().ApplyQuery(queryProcessor, options1).ToList();

        // Second query - should use cached compilation
        var options2 = new TestQueryOptions { Name = "Name200" };
        _testData.AsQueryable().ApplyQuery(queryProcessor, options2).ToList();

        // Third query with different fields
        var options3 = new TestQueryOptions { Fields = "Id,Name" };
        _testData.AsQueryable().ApplyQuery(queryProcessor, options3).ToList();
    }

    [Benchmark]
    public void WarmupSimulation_AllCommonQueries()
    {
        var queryProcessor = new QueryProcessor();
        queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Simulate warming up common query patterns
        var warmupQueries = new[]
        {
            new TestQueryOptions { Name = "Name1" },
            new TestQueryOptions { Fields = "Id,Name" },
            new TestQueryOptions { Sort = "Age" },
            new TestQueryOptions { Name = "Name1", Fields = "Id,Name,Age", Sort = "-Age" }
        };

        foreach (var options in warmupQueries)
        {
            _testData.AsQueryable().ApplyQuery(queryProcessor, options).Take(1).ToList();
        }
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
