using System.Diagnostics;

namespace WinTweaker.Services;

/// <summary>Simple CPU benchmark: single-core and multi-threaded score.</summary>
public static class Benchmark
{
    public static Task<(long Single, long Multi)> RunAsync() => Task.Run(() =>
    {
        long single = ScoreSingle();
        long multi = ScoreMulti();
        return (single, multi);
    });

    // CPU-bound math workload; return value prevents the loop being optimized away.
    private static double Work(long iterations)
    {
        double x = 0;
        for (long i = 0; i < iterations; i++)
            x += Math.Sqrt(i * 1.0001 + 1) + Math.Sin(i * 0.5);
        return x;
    }

    private static long ScoreSingle()
    {
        const long iters = 40_000_000;
        var sw = Stopwatch.StartNew();
        var r = Work(iters);
        sw.Stop();
        GC.KeepAlive(r);
        return Score(iters, sw.Elapsed.TotalSeconds);
    }

    private static long ScoreMulti()
    {
        int cores = Math.Max(1, Environment.ProcessorCount);
        const long itersPer = 40_000_000;
        var sw = Stopwatch.StartNew();
        Parallel.For(0, cores, _ => { var r = Work(itersPer); GC.KeepAlive(r); });
        sw.Stop();
        return Score(itersPer * cores, sw.Elapsed.TotalSeconds);
    }

    // Normalize ops/sec into a friendly "points" number.
    private static long Score(long ops, double seconds)
    {
        if (seconds <= 0) return 0;
        return (long)(ops / seconds / 100_000.0);
    }
}
