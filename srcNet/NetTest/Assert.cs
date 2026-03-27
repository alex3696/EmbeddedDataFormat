namespace NetTest;

public class Assert
{
    public static void IsTrue(object? actual, string? msg = default)
    {
        if (true == actual?.Equals(true))
            return;
        throw new Exception(msg);
    }
    public static void AreEqual<T>(T? expected, T? actual, string? msg = default)
    {
        if (expected is IEquatable<T> v1 && actual is IEquatable<T> v2)
        {
            if (true == v1.Equals(v2))
                return;
        }
        if (true == object.Equals(expected, actual))
            return;
        if (true == expected?.Equals(actual))
            return;
        throw new Exception($"msg={msg}, expected={expected} actual={actual}");
    }
    public static void Fail(string? msg = default)
    {
        throw new Exception(msg);
    }
    public static void IsNotNull<T>(T? expected, string? msg = default)
    {
        if (expected is null)
            throw new Exception(msg);
    }
}
