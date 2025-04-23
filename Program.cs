using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SimdBenchmarks;

#if DEBUG
var benchmarks = new Benchmarks { N = 128 };
benchmarks.Setup();
var archFilter = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "Arm" : "Intel";
var methods = typeof(Benchmarks).GetMethods()
    .Where(m => !m.Name.StartsWith(archFilter) && m.GetCustomAttributes(typeof(BenchmarkAttribute), false).Length > 0)
    .Select(m => $"{m.Name}: {m.Invoke(benchmarks, null)}");

foreach (var method in methods)
{
    Console.WriteLine(method);
}
#else
BenchmarkRunner.Run<Benchmarks>();
#endif


