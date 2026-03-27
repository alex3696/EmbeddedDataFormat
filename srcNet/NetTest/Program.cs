using NetTest;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            Test1.TestPrimitiveDecomposer();
            Test1.TestPackUnpack();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
        return 0;
    }
}
