using EdfNet.Converters;
using EdfNet.Interfaces;
using EdfNet.Utils;
using System.Diagnostics;

namespace NetTest;

[TestClass]
public class TestConverters
{
    public static readonly string FileName = "ConvertTest";
    public const int NCOUNT = 1_000_000;
    readonly string _binFile;
    readonly string _txtFile;
    public TestConverters()
    {
        _binFile = TestStructSerialize.GetTestFilePath($"{FileName}.bdf");
        _txtFile = TestStructSerialize.GetTestFilePath($"{FileName}.tdf");
        CreateBinFilesIfNotExist(_binFile, NCOUNT, st => new EdfBinaryWriter(st));
        CreateBinFilesIfNotExist(_txtFile, NCOUNT, st => new EdfTextWriter(st));
    }
    void CreateBinFilesIfNotExist(string fileName, int count, Func<Stream, IWriter> factory)
    {
        if (File.Exists(fileName))
            return;
        using var file = new FileStream(fileName, FileMode.Create);
        var _writerGen = factory.Invoke(file);
        _writerGen.WriteSchema(TestClasses_Content.KeyValSchema);
        for (int i = 0; i < count; i++)
        {
            _writerGen.WriteValue(TestClasses_Content.TestValue);
        }
        _writerGen.Flush();
        if (_writerGen is IDisposable d)
            d.Dispose();
    }
    public void BinToTxtConvert() => BinToTxt.Convert(_binFile, TestStructSerialize.GetTestFilePath($"{FileName}Conv.tdf"));
    public void TxtToBinConvert() => TxtToBin.Convert(_txtFile, TestStructSerialize.GetTestFilePath($"{FileName}Conv.bdf"));
    [TestMethod]
    public void TestBinToTxtConvert()
    {
        {
            using var src = new FileStream(_binFile, FileMode.Open, FileAccess.Read);
            using var dst = new FileStream(TestStructSerialize.GetTestFilePath($"{FileName}Conv.tdf"), FileMode.Create, FileAccess.Write);
            RunSingleTest("Bin >> Txt", () =>
            {
                BinToTxt.Convert(src, dst);
            });
        }
        bool isEqual = FileUtils.FileCompare(_txtFile, TestStructSerialize.GetTestFilePath($"{FileName}Conv.tdf"));
        Assert.IsTrue(isEqual);
    }
    [TestMethod]
    public void TestTxtToBinConvert()
    {
        {
            using var src = new FileStream(_txtFile, FileMode.Open, FileAccess.Read);
            using var dst = new FileStream(TestStructSerialize.GetTestFilePath($"{FileName}Conv.bdf"), FileMode.Create, FileAccess.Write);
            RunSingleTest("Txt >> Bin", () =>
            {
                TxtToBin.Convert(src, dst);
            });
        }
        bool isEqual = FileUtils.FileCompare(_binFile, TestStructSerialize.GetTestFilePath($"{FileName}Conv.bdf"));
        Assert.IsTrue(isEqual);
    }

    private static void RunSingleTest(string testName, Action testAction)
    {
        Console.WriteLine($"=== {testName} ===");

        // Принудительный GC перед тестом
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Замер памяти до
        var memBefore = GC.GetTotalMemory(true);

        // Замер времени
        var sw = Stopwatch.StartNew();
        testAction();
        sw.Stop();

        // Замер памяти после
        var memAfter = GC.GetTotalMemory(true);
        var memUsed = memAfter - memBefore;

        Console.WriteLine($"Time:     {sw.Elapsed.TotalSeconds:F3}s");
        Console.WriteLine($"Memory:   {memUsed / 1024.0:F2} KB ({memUsed:N0} bytes)");
        Console.WriteLine($"Gen0:     {GC.CollectionCount(0)}");
        Console.WriteLine($"Gen1:     {GC.CollectionCount(1)}");
        Console.WriteLine($"Gen2:     {GC.CollectionCount(2)}");
        Console.WriteLine();
    }
}

