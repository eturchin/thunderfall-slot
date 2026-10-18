# Performance evidence

Measured locally on Intel Core i5-14600KF (14 physical/20 logical cores), Windows 11 build 26200, SDK 10.0.401, runtime .NET 10.0.12 x64 RyuJIT. Definitions/PRNG/hash are recorded with simulation output. No production-capacity or regulated throughput claim is made.

## Complete-cycle workload

Frozen holdout: 50,000,000 paid cycles plus 5,041,155 bonus rounds, eight workers, 7.764018s elapsed, **6,439,964 cycles/s**. Process CPU time=55.0625s; average utilization of all 20 logical processors=35.46%. Global process managed allocation delta=704,353,432 bytes (14.087 B/paid cycle). This includes warm-up/feature/chunk statistics allocations, not database/HTTP/rendering work.

The 100k cycles/s design target was exceeded in this native workload. It does not imply equal API throughput; durable transactions and tracing have different costs.

## BenchmarkDotNet short steady-state sample

Seven workloads cover initial generation, frequency evaluation, gravity, weighted sampling, full-round trace off/on and one million complete paid cycles. Three warmups, three 100ms target iterations, one launch. Short-sample uncertainty is retained; use longer jobs on an idle machine before optimization conclusions. Source and artifacts are inspectable.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i5-14600KF 3.50GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-RELKCN : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

IterationCount=3  IterationTime=100ms  LaunchCount=1
WarmupCount=3

```
| Method            | Mean                | Error             | StdDev           | Gen0      | Allocated  |
|------------------ |--------------------:|------------------:|-----------------:|----------:|-----------:|
| BoardGeneration   |           452.38 ns |         43.858 ns |         2.404 ns |         - |          - |
| SymbolFrequency   |            24.64 ns |          1.181 ns |         0.065 ns |         - |          - |
| TumbleGravity     |           157.95 ns |         11.381 ns |         0.624 ns |         - |          - |
| WeightedSampling  |            13.84 ns |          0.191 ns |         0.010 ns |         - |          - |
| FullRoundTraceOff |           723.68 ns |         56.039 ns |         3.072 ns |         - |        1 B |
| FullRoundTraceOn  |         1,968.96 ns |        989.809 ns |        54.255 ns |    0.5266 |     6609 B |
| MillionPaidCycles | 1,381,544,800.00 ns | 43,903,947.918 ns | 2,406,523.542 ns | 1000.0000 | 14222576 B |


The separate `reports/benchmarks/` Dry job is a cold smoke run; its single-iteration/JIT-heavy times are not steady-state performance. MemoryDiagnoser figures are managed allocations, not total native/GPU memory. Whole-workload report uses GC.GetTotalAllocatedBytes across process threads. Results exclude JSON serialization inside ordinary simulation, database work and logging per spin.

Stack buffers, compact numeric cells, precompiled CDF/paytable lookups and trace-free execution reduce ordinary hot-path allocations. Traced/untraced equivalence and independent reference properties protect correctness. No unsafe pointer arithmetic or unverified alias optimization was needed.
