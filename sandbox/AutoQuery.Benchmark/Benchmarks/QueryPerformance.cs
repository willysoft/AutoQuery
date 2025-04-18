using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace AutoQuery.Benchmark.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class QueryPerformance
{
    private List<TestData> _autoQueryTestData = null!;
    private List<TestData> _dynamicLinqTestData = null!;
    private List<TestData> _sieveTestData = null!;
    private QueryProcessor _queryProcessor = null!;
    private SieveProcessor _sieveProcessor = null!;

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
        _sieveTestData = Enumerable.Range(1, 10000)
                                   .Select(i => new TestData { Id = i, Name = $"Name{i}", Age = i % 100 })
                                   .ToList();

        var sieveOptions = Options.Create(new SieveOptions
        {
            DefaultPageSize = 10,
            MaxPageSize = 100,
            CaseSensitive = false
        });

        _sieveProcessor = new SieveProcessor(sieveOptions);
    }

    [Benchmark]
    public void AutoQuery_FilterSortSelectPage()
    {
        var queryOptions = new TestQueryOptions
        {
            Name = "Name5000",
            Sort = "Age",
            Fields = "Id,Name",
            Page = 1,
            PageSize = 10
        };

        _autoQueryTestData.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void DynamicLinq_FilterSortSelectPage()
    {
        _dynamicLinqTestData.AsQueryable()
                 .Where("Name == @0", "Name5000")
                 .OrderBy("Age")
                 .Select("new (Id, Name)")
                 .Skip(0)
                 .Take(10)
                 .ToDynamicList();
    }

    [Benchmark]
    public void AutoQuery_FilterSortPage()
    {
        var queryOptions = new TestQueryOptions
        {
            Name = "Name5000",
            Sort = "Age",
            Page = 1,
            PageSize = 10
        };

        _autoQueryTestData.AsQueryable().ApplyQueryPaged(_queryProcessor, queryOptions).ToList();
    }

    [Benchmark]
    public void DynamicLinq_FilterSortPage()
    {
        _dynamicLinqTestData.AsQueryable()
                 .Where("Name == @0", "Name5000")
                 .OrderBy("Age")
                 .Skip(0)
                 .Take(10)
                 .ToDynamicList();
    }

    [Benchmark]
    public void Sieve_FilterSortPage()
    {
        var sieveModel = new SieveModel
        {
            Filters = "Name==Name5000",
            Sorts = "Age",
            Page = 1,
            PageSize = 10,
        };

        _sieveProcessor.Apply(sieveModel, _sieveTestData.AsQueryable()).ToList();
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
