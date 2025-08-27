using Automation.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Automation.Core.Tests.CustomAttributes;

namespace Automation.Core.Tests;

public class PipeBenchmark
{
    public static IEdge<byte[]>? StartPipe = null;
    public static byte[] Fluid = null!;

    [Params(10_000)]
    public int Iterations;

    [GlobalSetup]
    public void Setup()
    {
        var currentPipe = StartPipe;
        while (currentPipe != null)
        {
            currentPipe = currentPipe.Pump(Fluid);
        }
    }

    [Benchmark(Description = "Traverse and Pump chain")]
    public void BenchmarkTraversePump()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var currentPipe = StartPipe;
            while (currentPipe != null)
            {
                currentPipe = currentPipe.Pump(Fluid);
            }
        }
    }
}

[TestClass]
public abstract class AbstractPipeTests<TFluid>
{
    public abstract IEnumerable<object[]> GetTestCases();

    [DataTestMethod]
    [InstanceDynamicData(nameof(GetTestCases))]
    public void Pipe_ShouldPump_UnderThreshold(
        Func<IEdge<TFluid>> pipeFactory,
        TFluid fluid,
        double maxAvgMs)
    {
        var startPipe = pipeFactory();

        var benchmarkType = typeof(PipeBenchmark);
        benchmarkType.GetField(
                nameof(PipeBenchmark.StartPipe),
                BindingFlags.Public | BindingFlags.Static)!.SetValue(null, startPipe);

        benchmarkType.GetField(
                nameof(PipeBenchmark.Fluid),
                BindingFlags.Public | BindingFlags.Static)!.SetValue(null, fluid);

        var benchInstance = Activator.CreateInstance(benchmarkType)!;
        benchmarkType.GetProperty(
                nameof(PipeBenchmark.Iterations),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty)?
            .SetValue(benchInstance, 10_000);

        var summary = BenchmarkRunner.Run(benchmarkType);
        var report = summary.Reports
            .First(r => r.BenchmarkCase.Descriptor.WorkloadMethod.Name == nameof(PipeBenchmark.BenchmarkTraversePump));

        Assert.IsNotNull(report, "Benchmark report not found.");
        Assert.IsNotNull(report.ResultStatistics, "Benchmark result statistics is null.");

        double avgMs = (report.ResultStatistics).Mean / 1_000_000.0;

        Assert.IsTrue(
            avgMs <= maxAvgMs,
            $"{startPipe.GetType().Name} average chain pump time {avgMs:F2} ms exceeds threshold {maxAvgMs} ms.");
    }
}