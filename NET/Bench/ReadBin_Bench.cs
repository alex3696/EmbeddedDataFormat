using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EdfNet.Core;
using NetTest;
using System;
using System.IO;

namespace Bench;

/*
| Method  | Job            | Size | Mean         | Ratio | Allocated | Alloc Ratio |
|-------- |--------------- |----- |-------------:|------:|----------:|------------:|
| ReadGen | .NET 10.0      | 1    |     7.731 us |  1.09 |   1.48 KB |        1.00 |
| ReadGen | NativeAOT 10.0 | 1    |     2.282 us |  0.32 |   1.13 KB |        0.76 |
|         |                |      |              |       |           |             |
| ReadGen | .NET 10.0      | 1000 | 1,256.307 us |  1.00 | 411.05 KB |        1.00 |
| ReadGen | NativeAOT 10.0 | 1000 |   406.430 us |  0.32 | 379.97 KB |        0.92 |
*/

[MemoryDiagnoser(false)]
[HideColumns("Runtime", "Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class ReadBin_Bench
{
#pragma warning disable CS8618
    MemoryStream _msGen;
    //EdfBinaryWriter _writerGen;
    EdfBinaryReader _reader;
#pragma warning restore CS8618
    [Params(1, 1000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _msGen = new MemoryStream(Size * 1000);
        using EdfBinaryWriter _writerGen = new(_msGen);
        _writerGen.WriteSchema(TestClasses_Content.KeyValSchema);
        for (int i = 0; i < Size + 1; i++)
        {
            _writerGen.WriteValue(TestClasses_Content.TestValue);
        }
        _writerGen.Flush();

    }
    [IterationSetup]
    public void IterationSetup()
    {
        //if (!reader.ReadBlock())
        //    Assert.Fail("there are no block");
        //if (reader.GetBlockType() != EdfBlockType.Config)
        //    Assert.Fail("there are no config block");

        // Перед каждой итерацией бенчмарка возвращаем поток в начало
        _msGen.Position = 0;
        _reader = new(_msGen);

        // Пропускаем заголовок и схему, чтобы бенчмарк тестировал только чтение данных
        if (!_reader.ReadBlock())
            throw new Exception("there are no block");
        if (_reader.GetBlockType() != EdfBlockType.Schema)
            throw new Exception("there are no schema block");
        if (!TestClasses_Content.KeyValSchema.Equals(_reader.CurrentSchema))
            throw new Exception("schema equals");

        if (!_reader.ReadBlock())
            throw new Exception("there are no block");

    }

    [Benchmark(Baseline = true /*, OperationsPerInvoke = 1*/ )]
    public void ReadGen()
    {
        int block = 0;
        int i = Size;
        while (0 < i--)
        {
            if (0 < _reader.DataAvailable)
            {
                var restored = _reader.ReadValue<ComplexType>();
                if (!TestClasses_Content.TestValue.Equals(restored))
                    throw new Exception("schema equals");
            }
            else
            {
                if (!_reader.ReadBlock())
                    throw new Exception("there are no block");
                if (_reader.GetBlockType() != EdfBlockType.Data)
                    throw new Exception("there are no data block");
                block++;
            }
        }
        Console.WriteLine($"Blocks readed {block} Stream pos {_msGen.Position}");
    }
}
