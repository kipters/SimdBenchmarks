using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

#if DEBUG
var benchmarks = new Benchmarks { N = 128 };
benchmarks.Setup();
Console.WriteLine($"{benchmarks.ScalarCrc32()} {benchmarks.ArmCrc32()}");
#else
BenchmarkRunner.Run<Benchmarks>();
#endif

[DisassemblyDiagnoser]
public class Benchmarks
{
    private int[] data = [];
    private byte[] bytes = [];

    // [Params(128, 262144, 1048576)]
    public int N = 128;

    [GlobalSetup]
    public void Setup()
    {
        bytes = new byte[N];
        Random.Shared.NextBytes(bytes);
        
        data = [.. Enumerable
            .Range(0, ushort.MaxValue)
            .Take(N)];
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
        var n = data.Length / 4;
        for (var i = 0; i < n; i += 4)
        {
            sum += data[n];
            sum += data[n + 1];
            sum += data[n + 2];
            sum += data[n + 3];
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
    public unsafe uint ScalarCrc32()
    {
        const uint polynomial = 0xEDB88320;

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
            crc = Crc32.Arm64.ComputeCrc32(crc, value);
        }

        // Process remaining 4 bytes if possible
        for (; i <= bytes.Length - 4; i += 4)
        {
            uint value = BitConverter.ToUInt32(bytes, i);
            crc = Crc32.Arm64.ComputeCrc32(crc, value);
        }

        // Process remaining bytes
        for (; i < bytes.Length; i++)
        {
            crc = Crc32.Arm64.ComputeCrc32(crc, bytes[i]);
        }

        return ~crc; // Finalize CRC (inversion)
    }
}

