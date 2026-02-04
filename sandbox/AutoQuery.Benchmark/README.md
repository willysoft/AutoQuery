# AutoQuery Benchmark Suite

This benchmark suite provides comprehensive performance testing for the AutoQuery library, supporting the Performance Improvement Plan outlined in the project roadmap.

## Benchmark Categories

### 1. Cold Start Performance (`ColdStartBenchmark`)
**Purpose**: Measures first-query latency and expression compilation overhead

**Target Optimization** (Phase 1):
- Implement warmup mechanism during application startup
- Use `Lazy<T>` to ensure single compilation per expression
- Add API for manual pre-compilation of common queries
- **Estimated impact**: 50-70% reduction in first-query latency

**Scenarios**:
- `FirstQuery_WithConfiguration` - Cold start with full configuration
- `FirstQuery_SimpleFilter` - Cold start with simple filtering
- `FirstQuery_ComplexOperation` - Cold start with complex query

### 2. Concurrency Performance (`ConcurrencyBenchmark`)
**Purpose**: Tests thread safety and scalability under parallel load

**Configurations**: 1, 4, 8, 16 threads

**Scenarios**:
- `ParallelQueries_DifferentFilters` - Different filters per thread
- `ParallelQueries_SameFilter` - Same filter across threads (cache contention)
- `ParallelQueries_ComplexOperations` - Complex queries with different parameters

### 3. Memory Performance (`MemoryBenchmark`)
**Purpose**: Measures GC pressure and memory allocations

**Target Optimization** (Phase 1):
- Use `Span<char>` for zero-allocation string parsing
- Cache parsing results for Fields and Sort parameters
- **Estimated impact**: 30-40% reduction in GC pressure, 40-50% faster string processing

**Scenarios**:
- `Query_StringParsing_Fields` - Field selection parsing allocations
- `Query_StringParsing_Sort` - Sort parameter parsing allocations
- `Query_RepeatedFieldsParsing` - Tests caching effectiveness (100 iterations)
- `Query_RepeatedSortParsing` - Tests sort caching effectiveness (100 iterations)
- `Query_FullPipeline_WithAllocation` - Complete pipeline memory profile

### 4. Large Dataset Performance (`LargeDatasetBenchmark`)
**Purpose**: Tests scalability with large data volumes

**Dataset Sizes**: 100K, 500K, 1M records

**Scenarios**:
- `Query_100K_FilterSortPage` - Basic operations on 100K records
- `Query_100K_ComplexFilter` - Complex filtering on 100K records
- `Query_500K_FilterSortPage` - Basic operations on 500K records
- `Query_1M_FilterSortPage` - Basic operations on 1M records
- `Query_1M_FullScan` - Full table scan on 1M records
- `Query_1M_PagedResult` - Paginated results on 1M records

### 5. String Parsing Performance (`StringParsingBenchmark`)
**Purpose**: Detailed analysis of string parsing overhead

**Target Optimization** (Phase 1):
- Implement `Span<char>` based parsing
- Add parsing result caching
- **Estimated impact**: 40-50% faster string processing

**Scenarios**:
- `ParseFields_Simple` - Simple field list (2 fields)
- `ParseFields_Complex` - Complex field list (6 fields)
- `ParseFields_WithSpaces` - Field list with whitespace
- `ParseSort_Single` - Single sort field
- `ParseSort_Multiple` - Multiple sort fields (3 fields)
- `ParseSort_Complex` - Complex sort (6 fields with directions)
- `ParseFieldsAndSort_Combined` - Combined parsing
- `ParseFieldsAndSort_Repeated` - Cache effectiveness test (1000 iterations)
- `ParseFieldsAndSort_VariedInputs` - Varied inputs (100 variations)

### 6. Expression Compilation Performance (`ExpressionCompilationBenchmark`)
**Purpose**: Measures expression tree compilation overhead

**Target Optimization** (Phase 1):
- Implement Lazy<T> compilation
- Add warmup mechanism
- Provide pre-compilation API
- **Estimated impact**: 50-70% reduction in first-query latency

**Scenarios**:
- `CompileFilterExpression_FirstTime` - Filter compilation overhead
- `CompileSelectorExpression_FirstTime` - Selector compilation overhead
- `CompileSortExpression_FirstTime` - Sort compilation overhead
- `CompileComplexExpression_FirstTime` - Complex query compilation
- `CompileMultipleExpressions_Sequential` - Sequential compilation reuse
- `WarmupSimulation_AllCommonQueries` - Warmup simulation

### 7. Comparison Benchmarks (`QueryPerformance`)
**Purpose**: Compares AutoQuery with other libraries (System.Linq.Dynamic.Core, Sieve)

**Scenarios**:
- Filter + Sort + Select + Page operations
- Filter + Sort + Page operations

## Running Benchmarks

### Run All Benchmarks
```bash
cd sandbox/AutoQuery.Benchmark
dotnet run -c Release
```

### Run Specific Benchmark
```bash
dotnet run -c Release --filter "*ColdStartBenchmark*"
dotnet run -c Release --filter "*MemoryBenchmark*"
dotnet run -c Release --filter "*LargeDatasetBenchmark*"
```

### Run Specific Method
```bash
dotnet run -c Release --filter "*StringParsingBenchmark.ParseFields_Simple*"
```

### Export Results
```bash
dotnet run -c Release --exporters json,html,csv
```

## Interpreting Results

### Key Metrics

1. **Mean Time**: Average execution time per operation
2. **Allocated**: Memory allocated per operation
3. **Gen0/Gen1/Gen2**: Garbage collection counts
4. **Ratio**: Comparison to baseline (when available)

### Example Output
```
|                        Method |      Mean | Allocated |
|------------------------------ |----------:|----------:|
| Query_StringParsing_Fields   |  50.00 μs |   12.5 KB |
| Query_RepeatedFieldsParsing  | 500.00 μs |  125.0 KB |
```

### Performance Goals (Phase 1)

Based on benchmark results, track progress toward:
- **30-40% reduction** in GC pressure (Allocated, Gen0 collections)
- **40-50% faster** string processing (Mean time for StringParsingBenchmark)
- **50-70% reduction** in first-query latency (ColdStartBenchmark, ExpressionCompilationBenchmark)

## Benchmark Configuration

Current configuration (`BenchmarkConfig.cs`):
- **Runtime**: .NET 8.0
- **Mode**: ShortRun (for quick feedback)
- **WarmupCount**: 1
- **IterationCount**: 3
- **Diagnostics**: Memory Diagnoser enabled

For production benchmarking, consider:
```csharp
Job.Default
    .WithWarmupCount(3)
    .WithIterationCount(10)
```

## Adding New Benchmarks

1. Create a new class in the `Benchmarks` folder
2. Add `[Config(typeof(BenchmarkConfig))]` attribute
3. Add `[MemoryDiagnoser]` for memory metrics
4. Implement `[GlobalSetup]` for initialization
5. Add benchmark methods with `[Benchmark]` attribute

Example:
```csharp
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class MyBenchmark
{
    [GlobalSetup]
    public void Setup() { /* initialize */ }

    [Benchmark]
    public void MyScenario() { /* benchmark code */ }
}
```

## Continuous Performance Tracking

Recommendations:
1. Run benchmarks before and after each optimization
2. Track metrics in a spreadsheet or dashboard
3. Set up automated benchmark runs in CI/CD
4. Compare results against baseline before merging PRs
5. Document improvements in PR descriptions

## Related Documentation

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Improvement Plan](../../README.md#performance-improvement-plan)
- [.NET Performance Tips](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
