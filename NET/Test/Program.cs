using NetTest;

internal class Program
{
    public static int TestNo = 0;
    public static int TestResult = 0;
    public static void ExecTryCatch(Action act, string? title = default)
    {
        TestNo++;
        try
        {
            act.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{TestNo}. {title} failed: {ex}");
            TestResult++;
            return;
        }
        Console.WriteLine($"{TestNo}. {title} passed");
    }

    public static int Main(string[] args)
    {
        var reflTests = new TestStructSerialize();
        //ExecTryCatch(reflTests.TestPrimitiveDecomposer);
        ExecTryCatch(reflTests.TestPackUnpack);

        var genTests = new GenSerializationTests();
        ExecTryCatch(genTests.Generate_Schema);
        ExecTryCatch(genTests.KeyVal_Serialization_And_Deserialization_Should_Be_Identical);
        return TestResult;
    }
}
