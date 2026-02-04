using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

/// <summary>
/// Benchmarks for string parsing performance
/// Target for Phase 1 optimization: Use Span<char> for zero-allocation string parsing
/// Estimated impact: 40-50% faster string processing, 30-40% reduction in GC pressure
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class StringParsingBenchmark
{
    private List<TestData> _testData = null!;
    private QueryProcessor _queryProcessor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _queryProcessor = new QueryProcessor();
        _queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        _testData = Enumerable.Range(1, 1000)
                             .Select(i => new TestData 
                             { 
                                 Id = i, 
                                 Name = $"Name{i}", 
                                 Age = i % 100,
                                 Email = $"user{i}@example.com",
                                 Phone = $"555-{i:D4}",
                                 Address = $"{i} Main St"
                             })
                             .ToList();
    }

    [Benchmark]
    public void ParseFields_Simple()
    {
        var queryOptions = new TestQueryOptions
        {
            Fields = "Id,Name"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseFields_Complex()
    {
        var queryOptions = new TestQueryOptions
        {
            Fields = "Id,Name,Age,Email,Phone,Address"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseFields_WithSpaces()
    {
        var queryOptions = new TestQueryOptions
        {
            Fields = "Id, Name, Age, Email, Phone, Address"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseSort_Single()
    {
        var queryOptions = new TestQueryOptions
        {
            Sort = "Name"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseSort_Multiple()
    {
        var queryOptions = new TestQueryOptions
        {
            Sort = "-Age,Name,Id"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseSort_Complex()
    {
        var queryOptions = new TestQueryOptions
        {
            Sort = "-Age,Name,-Email,Phone,-Id,Address"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseFieldsAndSort_Combined()
    {
        var queryOptions = new TestQueryOptions
        {
            Fields = "Id,Name,Age,Email",
            Sort = "-Age,Name,-Id"
        };

        _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void ParseFieldsAndSort_Repeated()
    {
        // Simulates repeated parsing of same strings (tests caching effectiveness)
        for (int i = 0; i < 1000; i++)
        {
            var queryOptions = new TestQueryOptions
            {
                Fields = "Id,Name,Age",
                Sort = "-Age,Name"
            };

            _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).Take(1).ToList();
        }
    }

    [Benchmark]
    public void ParseFieldsAndSort_VariedInputs()
    {
        // Simulates varying inputs (worst case for caching)
        for (int i = 0; i < 100; i++)
        {
            var fieldCount = (i % 6) + 1;
            var fields = string.Join(",", Enumerable.Range(0, fieldCount)
                .Select(j => new[] { "Id", "Name", "Age", "Email", "Phone", "Address" }[j]));
            
            var queryOptions = new TestQueryOptions
            {
                Fields = fields,
                Sort = i % 2 == 0 ? "Age" : "-Age"
            };

            _testData.AsQueryable().ApplyQuery(_queryProcessor, queryOptions).Take(1).ToList();
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
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
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
