namespace EdfNet.Utils;

public class Testing
{
    public static void RunSingleTest(string testName, Action testAction)
    {
        Console.WriteLine($"=== {testName} ===");

        // Принудительный GC перед тестом
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Замер памяти до
        var memBefore = GC.GetAllocatedBytesForCurrentThread();// GC.GetTotalMemory(true);

        // Замер времени
        var sw = Stopwatch.StartNew();
        testAction();
        sw.Stop();

        // Замер памяти после
        var memAfter = GC.GetAllocatedBytesForCurrentThread(); //GC.GetTotalMemory(true);
        var memUsed = memAfter - memBefore;

        Console.WriteLine($"Time:     {sw.Elapsed.TotalSeconds:F3}s");
        Console.WriteLine($"Memory:   {memUsed / 1024.0:F2} KB ({memUsed:N0} bytes)");
        Console.WriteLine($"Gen0:     {GC.CollectionCount(0)}");
        Console.WriteLine($"Gen1:     {GC.CollectionCount(1)}");
        Console.WriteLine($"Gen2:     {GC.CollectionCount(2)}");
        Console.WriteLine();
    }
}
