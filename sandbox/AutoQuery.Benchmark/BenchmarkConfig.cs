using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace AutoQuery.Benchmark;

internal class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        
        // ShortRun for quick feedback during development
        // For production benchmarks, use Job.Default with more iterations
        AddJob(Job.ShortRun
            .WithWarmupCount(1)
            .WithIterationCount(3)
            .WithRuntime(CoreRuntime.Core80));

        // Display summary with statistics
        SummaryStyle = SummaryStyle.Default
            .WithRatioStyle(RatioStyle.Trend);
    }
}
