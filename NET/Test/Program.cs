using Test;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            ReflectionTest_1.TestPrimitiveDecomposer();
            ReflectionTest_1.TestPackUnpack();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
        return 0;
    }
}
