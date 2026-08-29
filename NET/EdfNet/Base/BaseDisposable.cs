namespace EdfNet.Base;

public class BaseDisposable : IDisposable
{
    public delegate void LogMessage(string? msg);

    public LogMessage Log = ConsoleLogMessage;

    public static void ConsoleLogMessage(string? msg)
    {
        DebugLogMessage(msg);
        Console.WriteLine(msg);
    }
    [Conditional("DEBUG")]
    public static void DebugLogMessage(string? msg) => Debug.WriteLine(msg);


    private int _isDisposed = 0;
    public bool IsDisposed => 0 != _isDisposed;

    ~BaseDisposable()
    {
        if (IsDisposed)
            return;
        Log($"MEMORY LEAK: {this.GetType().FullName}");
    }
    public void Dispose()
    {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }
    private void DisposeInternal()
    {
        if (0 != _isDisposed)
            return;
        _isDisposed = 1;
        try
        {
            Dispose(true);
        }
        catch (Exception ex)
        {
            Log($"FAILED Dispose {ex}");
        }
    }
    protected virtual void Dispose(bool disposing)
    {
    }
}
