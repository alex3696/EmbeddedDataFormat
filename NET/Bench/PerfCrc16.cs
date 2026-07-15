using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EdfNet.Core;
using System;
using System.Linq;
using System.Text;

namespace TestPerfomance;

[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev", "Median")]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class PerfCrc16
{
    // Главный массив-источник
    private static readonly byte[] _masterArray = Encoding.ASCII.GetBytes(
        string.Concat(Enumerable.Repeat(
        "123456789 123456789 123456789 123456789 123456789 " +
        "123456789 123456789 123456789 123456789 123456789 ", 6))
    );

    // Размеры буферов для тестирования
    [Params(32, 250, 512)]
    public int Size { get; set; }

    // Храним данные в виде обычного массива (разрешено для полей класса)
    private byte[]? _currentArray;
    private ushort _expectedCrc;

    [GlobalSetup]
    public void Setup()
    {
        // Вырезаем массив нужного размера для текущего теста
        _currentArray = new byte[Size];
        Array.Copy(_masterArray, _currentArray, Size);

        // Считаем эталонный CRC один раз при старте
        _expectedCrc = ModbusCRC.Calc(_currentArray);
    }

    // Делегат теперь принимает ReadOnlySpan<byte>, чтобы бенчмарк не аллоцировал память
    public void Check(ushort expected, Func<ReadOnlySpan<byte>, ushort> fn)
    {
        // Создаем Span из массива прямо в стеке во время валидации
        if (expected != fn(_currentArray))
            throw new Exception("CRC error");
    }

    // В каждом бенчмарке передаем массив, который неявно или явно приводится к ReadOnlySpan<byte>
    [Benchmark(Baseline = true)] public void Crc16Fn() => Check(_expectedCrc, (buf) => ModbusCRC.CalcFn(buf));

    //[Benchmark] public void Crc16Table01() => Check(_expectedCrc, (buf) => ModbusCRC.CalcTR(buf));

    [Benchmark] public void Crc16RightShiftCalc() => Check(_expectedCrc, (buf) => ModbusCRC.CalcTF(buf));

    [Benchmark] public void Crc16Slicing8() => Check(_expectedCrc, (buf) => ModbusCRC.Calc(buf));
}
