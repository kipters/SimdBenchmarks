using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace SimdBenchmarks;

[DisassemblyDiagnoser]
[Config(typeof(Config))]
public class Benchmarks
{
    private int[] data = [];
    private byte[] bytes = [];
    public double[] A = [];
    public double[] B = [];
    private readonly ulong word = (ulong)Random.Shared.NextInt64();

    [Params(128, 262144, 1048576)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        bytes = new byte[N];
        Random.Shared.NextBytes(bytes);

        data = [.. Enumerable.Range(0, N).Select(_ => Random.Shared.Next() & 0x000000FF)];

        A = [.. Enumerable.Range(0, N).Select(_ => 2f * Random.Shared.NextSingle() - 1f)];

        B = [.. Enumerable.Range(0, N).Select(_ => 2f * Random.Shared.NextSingle() - 1f)];
    }

    public class Config : ManualConfig
    {
        public Config()
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            {
                AddFilter(new NameFilter(n => !n.StartsWith("Arm")));
            }

            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                AddFilter(new NameFilter(n => !n.StartsWith("Intel")));
            }
        }
    }

    [Benchmark]
    public int For()
    {
        var sum = 0;
        var n = data.Length;
        for (var i = 0; i < n; i++)
        {
            sum += data[i];
        }
        return sum;
    }

    [Benchmark]
    public int ForPacked()
    {
        var sum = 0;
        var n = data.Length;
        for (var i = 0; i < n; i += 4)
        {
            sum += data[i];
            sum += data[i + 1];
            sum += data[i + 2];
            sum += data[i + 3];
        }
        return sum;
    }

    [Benchmark]
    public int Foreach()
    {
        var sum = 0;
        foreach (var n in data)
        {
            sum += n;
        }
        return sum;
    }

    [Benchmark]
    public int Linq()
    {
        return data.Sum();
    }

    [Benchmark]
    public unsafe int Vectorized128()
    {
        var acc = Vector128<int>.Zero;
        ref int ptr = ref MemoryMarshal.GetReference<int>(data);

        ref int endMinusOne = ref Unsafe.Add(ref ptr, data.Length - Vector128<int>.Count);

        Vector128<int> v;
        do
        {
            v = Vector128.LoadUnsafe(ref ptr);
            acc += v;

            ptr = ref Unsafe.Add(ref ptr, Vector128<int>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptr, ref endMinusOne));

        v = Vector128.LoadUnsafe(ref ptr);
        acc += v;
        return Vector128.Sum(acc);
    }

    [Benchmark]
    public unsafe int Vectorized64()
    {
        var acc = Vector64<int>.Zero;
        ref int ptr = ref MemoryMarshal.GetReference<int>(data);

        ref int endMinusOne = ref Unsafe.Add(ref ptr, data.Length - Vector64<int>.Count);

        Vector64<int> v;
        do
        {
            v = Vector64.LoadUnsafe(ref ptr);
            acc += v;

            ptr = ref Unsafe.Add(ref ptr, Vector64<int>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptr, ref endMinusOne));

        v = Vector64.LoadUnsafe(ref ptr);
        acc += v;
        return Vector64.Sum(acc);
    }

    [Benchmark]
    public unsafe int Vectorized256()
    {
        var acc = Vector256<int>.Zero;
        ref int ptr = ref MemoryMarshal.GetReference<int>(data);

        ref int endMinusOne = ref Unsafe.Add(ref ptr, data.Length - Vector256<int>.Count);

        Vector256<int> v;
        do
        {
            v = Vector256.LoadUnsafe(ref ptr);
            acc += v;

            ptr = ref Unsafe.Add(ref ptr, Vector256<int>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptr, ref endMinusOne));

        v = Vector256.LoadUnsafe(ref ptr);
        acc += v;
        return Vector256.Sum(acc);
    }

    [Benchmark]
    public unsafe int Vectorized512()
    {
        var acc = Vector512<int>.Zero;
        ref int ptr = ref MemoryMarshal.GetReference<int>(data);

        ref int endMinusOne = ref Unsafe.Add(ref ptr, data.Length - Vector512<int>.Count);

        Vector512<int> v;
        do
        {
            v = Vector512.LoadUnsafe(ref ptr);
            acc += v;

            ptr = ref Unsafe.Add(ref ptr, Vector512<int>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptr, ref endMinusOne));

        v = Vector512.LoadUnsafe(ref ptr);
        acc += v;
        return Vector512.Sum(acc);
    }

    [Benchmark]
    public unsafe int VectorizedVariable()
    {
        var acc = Vector<int>.Zero;
        ref int ptr = ref MemoryMarshal.GetReference<int>(data);
        ref int endMinusOne = ref Unsafe.Add(ref ptr, data.Length - Vector<int>.Count);

        Vector<int> v;
        do
        {
            v = Vector.LoadUnsafe(ref ptr);
            acc += v;

            ptr = ref Unsafe.Add(ref ptr, Vector<int>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptr, ref endMinusOne));

        v = Vector.LoadUnsafe(ref ptr);
        acc += v;
        return Vector.Sum(acc);
    }

    [Benchmark]
    public unsafe uint ScalarCrc32()
    {
        const uint polynomial = 0x1EDC6F41;

        uint crc = 0xFFFFFFFF;

        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            crc ^= b;

            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }

    [Benchmark]
    public unsafe uint IntelCrc32()
    {
        uint crc = 0xFFFFFFFF; // Initial value (standard for CRC32)

        int i = 0;

        // Process 8 bytes (64 bits) at a time
        for (; i <= bytes.Length - 8; i += 8)
        {
            ulong value = BitConverter.ToUInt64(bytes, i);
            crc = (uint)Sse42.X64.Crc32(crc, value); // 64-bit CRC32 for 64-bit chunks
        }

        // Process 4 bytes (32 bits) at a time if possible
        for (; i <= bytes.Length - 4; i += 4)
        {
            uint value = BitConverter.ToUInt32(bytes, i);
            crc = Sse42.Crc32(crc, value); // 32-bit CRC32 for 32-bit chunks
        }

        // Process remaining bytes (1 byte at a time)
        for (; i < bytes.Length; i++)
        {
            crc = Sse42.Crc32(crc, bytes[i]);
        }

        return ~crc;
    }

    [Benchmark]
    public unsafe uint ArmCrc32()
    {
        uint crc = 0xFFFFFFFF;
        int i = 0;

        // Process 8 bytes at a time if possible
        for (; i <= bytes.Length - 8; i += 8)
        {
            ulong value = BitConverter.ToUInt64(bytes, i);
            crc = Crc32.Arm64.ComputeCrc32C(crc, value);
        }

        // Process remaining 4 bytes if possible
        for (; i <= bytes.Length - 4; i += 4)
        {
            uint value = BitConverter.ToUInt32(bytes, i);
            crc = Crc32.Arm64.ComputeCrc32C(crc, value);
        }

        // Process remaining bytes
        for (; i < bytes.Length; i++)
        {
            crc = Crc32.Arm64.ComputeCrc32C(crc, bytes[i]);
        }

        return ~crc; // Finalize CRC (inversion)
    }

    [Benchmark]
    public int LeetPopolationCount()
    {
        var x = word;
        // Hacker's Delight / Bit Twiddling Hack method
        x -= (x >> 1) & 0x5555555555555555UL;
        x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
        x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
        x += x >> 8;
        x += x >> 16;
        x += x >> 32;

        return (int)(x & 0x7F); // Max possible popcount is 64
    }

    [Benchmark]
    public int PopolationCount()
    {
        var count = 0;
        var x = word;

        for (var i = 0; i < 64; i++)
        {
            if ((x & 1) == 1)
            {
                count++;
            }
            x >>= 1;
        }

        return count;
    }

    [Benchmark] public int IntrinsicPopCount() => BitOperations.PopCount(word);
    [Benchmark] public int IntelPopCount() => (int)Popcnt.X64.PopCount(word);
    [Benchmark] public int ArmPopCount64()
    {
        return Vector64.Sum(AdvSimd.PopCount(Vector64.Create(word).AsByte()));
    }

    [Benchmark] public int ArmPopCount128()
    {
        Span<byte> buffer = stackalloc byte[16];
        BitConverter.TryWriteBytes(buffer, word);
        ref byte ptr = ref MemoryMarshal.GetReference(buffer);
        var v = Vector128.LoadUnsafe(ref ptr);
        return Vector128.Sum(AdvSimd.PopCount(v));
    }

    [Benchmark]
    public double DotScalar()
    {
        double acc = 0;

        for (int i = 0; i < N; i++)
        {
            acc += A[i] * (double)B[i];
        }

        return acc;
    }

    [Benchmark]
    public double DotLinq() => A
        .Zip(B)
        .Select(t => t.First * (double) t.Second)
        .Sum();

    [Benchmark]
    public double DotIntrinsic()
    {
        double acc = 0;

        ref double ptrA = ref MemoryMarshal.GetReference<double>(A);
        ref double ptrB = ref MemoryMarshal.GetReference<double>(B);

        ref double endMinusOne = ref Unsafe.Add(ref ptrA, A.Length - Vector256<double>.Count);

        Vector256<double> a, b;

        do
        {
            a = Vector256.LoadUnsafe(ref ptrA);
            b = Vector256.LoadUnsafe(ref ptrB);

            acc += Vector256.Dot(a, b);

            ptrA = ref Unsafe.Add(ref ptrA, Vector256<double>.Count);
            ptrB = ref Unsafe.Add(ref ptrB, Vector256<double>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptrA, ref endMinusOne));

        a = Vector256.LoadUnsafe(ref ptrA);
        b = Vector256.LoadUnsafe(ref ptrB);

        acc += Vector256.Dot(a, b);

        return acc;
    }

    [Benchmark]
    public unsafe double DotVector256()
    {
        var acc = Vector256<double>.Zero;

        ref double ptrA = ref MemoryMarshal.GetReference<double>(A);
        ref double ptrB = ref MemoryMarshal.GetReference<double>(B);

        ref double endMinusOne = ref Unsafe.Add(ref ptrA, A.Length - Vector256<double>.Count);

        Vector256<double> a, b;

        do
        {
            a = Vector256.LoadUnsafe(ref ptrA);
            b = Vector256.LoadUnsafe(ref ptrB);

            acc += a * b;

            ptrA = ref Unsafe.Add(ref ptrA, Vector256<double>.Count);
            ptrB = ref Unsafe.Add(ref ptrB, Vector256<double>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptrA, ref endMinusOne));

        a = Vector256.LoadUnsafe(ref ptrA);
        b = Vector256.LoadUnsafe(ref ptrB);

        acc += a * b;

        return Vector256.Sum(acc);
    }

    [Benchmark]
    public unsafe double DotVector128()
    {
        var acc = Vector128<double>.Zero;

        ref double ptrA = ref MemoryMarshal.GetReference<double>(A);
        ref double ptrB = ref MemoryMarshal.GetReference<double>(B);

        ref double endMinusOne = ref Unsafe.Add(ref ptrA, A.Length - Vector128<double>.Count);

        Vector128<double> a, b;

        do
        {
            a = Vector128.LoadUnsafe(ref ptrA);
            b = Vector128.LoadUnsafe(ref ptrB);

            acc += a * b;

            ptrA = ref Unsafe.Add(ref ptrA, Vector128<double>.Count);
            ptrB = ref Unsafe.Add(ref ptrB, Vector128<double>.Count);
        }
        while (Unsafe.IsAddressLessThan(ref ptrA, ref endMinusOne));

        a = Vector128.LoadUnsafe(ref ptrA);
        b = Vector128.LoadUnsafe(ref ptrB);

        acc += a * b;

        return Vector128.Sum(acc);
    }

    [Benchmark]
    public int MinimumScalar()
    {
        var min = int.MaxValue;

        for (var i = 0; i < N; i++)
        {
            if (data[i] < min)
            {
                min = data[i];
            }
        }

        return min;
    }

    [Benchmark]
    public int MinimumLinq() => data.Min();
}
