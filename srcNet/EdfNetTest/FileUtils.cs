namespace NetEdfTest;

public class FileUtils
{
    public static bool FileCompare(string file1, string file2)
    {
        using var s1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
        using var s2 = new FileStream(file2, FileMode.Open, FileAccess.Read);
        var b1 = new byte[1024];
        var b2 = new byte[1024];
        int r1, r2;
        do
        {
            r1 = s1.Read(b1);
            r2 = s2.Read(b2);
            if (!b1.AsSpan(0, r1).SequenceEqual(b2.AsSpan(0, r2)))
                return false;
        }
        while (0 != r1 && 0 != r2);
        return true;
    }

    public static byte[] GetRandom(int count)
    {
        var rnd = new Random();
        var b = new byte[count];
        //rnd.NextBytes(b);
        for (int i = 0; i < count; ++i)
            b[i] = (byte)rnd.Next(32, 127);
        return b;
    }
}
