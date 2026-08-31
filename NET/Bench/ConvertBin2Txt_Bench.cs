using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EdfNet.Converters;
using EdfNet.Core;
using EdfNet.Interfaces;
using NetTest;
using System;
using System.IO;

namespace Bench;

/*
| Method           | Runtime        | Mean    | Ratio | Allocated | Alloc Ratio |
|----------------- |--------------- |--------:|------:|----------:|------------:|
| 'Binary -> Text' | .NET 10.0      | 1.384 s |  1.00 |  16.68 KB |        1.00 |
| 'Binary -> Text' | NativeAOT 10.0 | 1.816 s |  1.31 |   25.8 KB |        1.55 |
|                  |                |         |       |           |             |
| 'Text -> Binary' | .NET 10.0      | 1.997 s |  1.00 |  14.64 KB |        1.00 |
| 'Text -> Binary' | NativeAOT 10.0 | 2.515 s |  1.26 |  26.05 KB |        1.78 |
 */

[MemoryDiagnoser(true)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
//[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.HostProcess, baseline: false)]
[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.NativeAot10_0, baseline: false)]
public class ConvertBin2Txt_Bench
{
    public const int NCOUNT = 1_000_000;
    public static readonly string FileName = "ConvertTest";
#pragma warning disable CS8618
    private string _binFile;
    private string _txtFile;
    private string _tempFile;
#pragma warning restore CS8618
    [GlobalSetup]
    public void Setup()
    {
        _binFile = TestStructSerialize.GetTestFilePath($"{FileName}.bdf");
        _txtFile = TestStructSerialize.GetTestFilePath($"{FileName}.tdf");

        // Создаем тестовые файлы если их нет
        if (!File.Exists(_binFile))
            CreateFile(_binFile, st => new EdfBinaryWriter(st));

        if (!File.Exists(_txtFile))
            CreateFile(_txtFile, st => new EdfTextWriter(st));
    }
    private void CreateFile(string fileName, Func<Stream, IWriter> factory)
    {
        using var file = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = factory.Invoke(file);
        writer.WriteSchema(TestClasses_Content.KeyValSchema);

        for (int i = 0; i < NCOUNT; i++)
            writer.WriteValue(TestClasses_Content.TestValue);

        writer.Flush();
        if (writer is IDisposable d)
            d.Dispose();
    }

    [Benchmark(Description = "Binary -> Text")]
    public void BinToTxtConvert()
    {
        _tempFile = TestStructSerialize.GetTestFilePath($"{FileName}_temp_{Guid.NewGuid()}.tdf");
        try
        {
            BinToTxt.Convert(_binFile, _tempFile);

            // Проверка (опционально, можно закомментировать для скорости)
            // bool isEqual = FileUtils.FileCompare(_txtFile, _tempFile);
            // if (!isEqual) throw new Exception("Files not equal");
        }
        finally
        {
            // Очистка
            try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
        }
    }

    [Benchmark(Description = "Text -> Binary")]
    public void TxtToBinConvert()
    {
        _tempFile = TestStructSerialize.GetTestFilePath($"{FileName}_temp_{Guid.NewGuid()}.bdf");
        try
        {
            TxtToBin.Convert(_txtFile, _tempFile);

            // bool isEqual = FileUtils.FileCompare(_binFile, _tempFile);
            // if (!isEqual) throw new Exception("Files not equal");
        }
        finally
        {
            try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
        }
    }
}

