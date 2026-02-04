# AutoQuery Performance Baseline - Benchmark Results

## Executive Summary

This document presents the baseline performance metrics for the AutoQuery library, measured using the comprehensive benchmark suite. These results establish a baseline for tracking performance improvements outlined in the Performance Improvement Plan.

**Test Environment:**
- **OS:** Ubuntu 24.04.3 LTS (Noble Numbat)
- **CPU:** AMD EPYC 7763, 4 logical cores, 2 physical cores
- **Runtime:** .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX2
- **Date:** 2026-02-04

## Key Performance Metrics

### 1. Cold Start Performance

Cold start latency (creating new QueryProcessor for each query):

| Scenario | Mean | Allocated | Gen0 | Gen1 |
|----------|------|-----------|------|------|
| Simple Filter | 1.052 ms | 66.54 KB | 3.91 | 1.95 |
| Complex Operation | 1.326 ms | 81.89 KB | 3.91 | 1.95 |
| Full Configuration | 1.536 ms | 88.77 KB | 3.91 | 1.95 |

**Key Insights:**
- First query latency ranges from **1.0 to 1.5 ms**
- Memory allocations: **66-89 KB** per cold start
- Consistent GC pressure across all scenarios

**Phase 1 Target:** Reduce first-query latency by 50-70% through:
- Lazy compilation with `Lazy<T>`
- Warmup mechanism during startup
- Pre-compilation API for common queries

### 2. String Parsing Performance

String parsing is a critical path with significant optimization potential:

| Operation | Mean | Allocated | Gen0 | Gen1 |
|-----------|------|-----------|------|------|
| Parse Simple Fields (2) | 355.9 μs | 83.17 KB | 4.88 | 4.39 |
| Parse Complex Fields (6) | 475.9 μs | 84.75 KB | 4.88 | 3.91 |
| Parse Single Sort | 1,135.4 μs | 49.74 KB | 1.95 | - |
| Parse Multiple Sort (3) | 1,018.8 μs | 77.64 KB | 3.91 | 1.95 |
| Parse Complex Sort (6) | 1,276.4 μs | 129.89 KB | 7.81 | 5.86 |
| Combined Fields+Sort | 1,197.2 μs | 140.92 KB | 7.81 | 5.86 |

**Repeated Parsing (Cache Effectiveness Test):**
- 1000 iterations with same fields/sort: **823.2 ms** (113 MB allocated)
- 100 iterations with varied inputs: **84.0 ms** (9.6 MB allocated)

**Key Insights:**
- String parsing allocates **49-141 KB** per operation
- Significant GC pressure (Gen0: 4.88-7.81, Gen1: 3.91-5.86)
- **NO CACHING** currently implemented (high allocations on repeated parsing)

**Phase 1 Target:** 40-50% faster parsing, 30-40% less GC pressure through:
- `Span<char>` zero-allocation parsing
- Caching parsed Fields and Sort parameters
- Expected impact: 823ms → ~411ms for repeated parsing (potential 50%+ improvement)

### 3. Memory Performance - String Operations

| Scenario | Mean | Allocated | Gen0 | Gen1 | Gen2 |
|----------|------|-----------|------|------|------|
| Fields Parsing (10K records) | 949.9 μs | 738.69 KB | 41.02 | 41.02 | 41.02 |
| Sort Parsing (10K records) | 5,939.1 μs | 563.76 KB | 39.06 | 39.06 | 39.06 |

**Key Insights:**
- **High Gen2 collections** indicate promotion to long-lived objects
- Sort parsing is **6.3x slower** than fields parsing
- Significant memory pressure: **563-739 KB** per 10K record query

**Phase 1 Target:** 
- Reduce Gen2 collections through better memory management
- Implement pooling for temporary buffers

## Performance Improvement Opportunities

### Phase 1: Quick Wins (Highest Priority)

#### 1. String Parsing Optimization ⭐⭐⭐
**Current State:**
- ParseFieldsAndSort_Repeated: 823 ms, 113 MB allocated
- No caching mechanism
- High GC pressure (Gen0/Gen1/Gen2 collections)

**Target:**
- 40-50% faster parsing
- 30-40% reduction in GC pressure
- Implement `Span<char>` parsing
- Add caching for parsed fields/sort

**Expected Impact:** High - String parsing is called on every query

#### 2. Expression Compilation Cache Warming ⭐⭐⭐
**Current State:**
- First query: 1.0-1.5 ms latency
- 66-89 KB allocated per cold start
- No warmup mechanism

**Target:**
- 50-70% reduction in first-query latency
- Lazy compilation
- Warmup API

**Expected Impact:** High - Critical for application startup

#### 3. Memory Pooling ⭐⭐
**Current State:**
- 563-739 KB allocated per query
- High Gen2 collections
- No object pooling

**Target:**
- 25-35% reduction in GC pressure
- Use ArrayPool<T> and MemoryPool<T>

**Expected Impact:** Medium - Reduces GC overhead

## Benchmark Usage Guide

### Running Specific Benchmark Suites

```bash
# Navigate to benchmark directory
cd sandbox/AutoQuery.Benchmark

# Run all benchmarks
dotnet run -c Release

# Run specific benchmark class
dotnet run -c Release --filter "*ColdStartBenchmark*"
dotnet run -c Release --filter "*StringParsingBenchmark*"
dotnet run -c Release --filter "*MemoryBenchmark*"
dotnet run -c Release --filter "*LargeDatasetBenchmark*"
dotnet run -c Release --filter "*ConcurrencyBenchmark*"
dotnet run -c Release --filter "*ExpressionCompilationBenchmark*"

# Run specific benchmark method
dotnet run -c Release --filter "*StringParsingBenchmark.ParseFieldsAndSort_Repeated*"

# Export results to multiple formats
dotnet run -c Release --exporters json,html,csv
```

### Interpreting Results

**Key Metrics:**
- **Mean:** Average execution time
- **Error/StdDev:** Measurement variation
- **Allocated:** Memory allocated per operation
- **Gen0/Gen1/Gen2:** Garbage collection frequency

**Look for:**
- High allocation values → Memory optimization needed
- High Gen0/Gen1 → Short-lived object pressure
- High Gen2 → Long-lived object pressure
- Large StdDev → Performance inconsistency

### Tracking Improvements

After implementing optimizations, run benchmarks and compare:

1. **Before optimization:** Record baseline metrics
2. **Implement changes:** Make targeted improvements
3. **After optimization:** Re-run same benchmarks
4. **Calculate impact:** 
   - Time improvement: `(Before - After) / Before * 100%`
   - Memory improvement: `(BeforeAlloc - AfterAlloc) / BeforeAlloc * 100%`
   - GC reduction: Compare Gen0/Gen1/Gen2 counts

## Next Steps

1. **Phase 1 Implementation:**
   - [ ] Implement Span<char> string parsing
   - [ ] Add parsing result caching
   - [ ] Implement Lazy<T> expression compilation
   - [ ] Create warmup API
   - [ ] Add memory pooling

2. **Continuous Measurement:**
   - Run benchmarks before/after each optimization
   - Track metrics in performance dashboard
   - Set up CI/CD benchmark automation
   - Include benchmark results in PR descriptions

3. **Expand Coverage:**
   - Add large dataset benchmarks (100K+, 1M+ records)
   - Add concurrency benchmarks
   - Add real-world scenario benchmarks

## Benchmark Results Location

Results are exported to:
- **CSV:** `BenchmarkDotNet.Artifacts/results/*-report.csv`
- **HTML:** `BenchmarkDotNet.Artifacts/results/*-report.html`
- **GitHub MD:** `BenchmarkDotNet.Artifacts/results/*-report-github.md`

## References

- [Performance Improvement Plan](../../README.md)
- [Benchmark Suite Documentation](README.md)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
