namespace Lab1.Common;

using System;
using System.Diagnostics;

public static class Metric
{

    public static void Measure(string taskName, Func<CounterResult> work)
    {
        GC.Collect();
        var sw = Stopwatch.StartNew();
        CounterResult result = work();
        sw.Stop();
        var process = Process.GetCurrentProcess();
        process.Refresh();
        double peakWorkingSetMb = process.PeakWorkingSet64 / (1024.0 * 1024.0);

        Console.WriteLine($"--- {taskName} ---");
        Console.WriteLine($"Result: Neg={result.Negative:N0}, Pos={result.Positive:N0}, Zero={result.Zero:N0} (Total={result.Total:N0})");
        Console.WriteLine($"Check:  {(result.Total == DataGenerator.TotalNumbers ? "[PASS]" : "[FAIL]")}");
        Console.WriteLine($"Time:   {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F3} s)");
        Console.WriteLine($"Memory: {peakWorkingSetMb:F2} MB");
        Console.WriteLine();


    }
}