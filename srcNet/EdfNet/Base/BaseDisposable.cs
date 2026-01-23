namespace NetEdf.Base;

public class BaseDisposable : IAnyDisposable
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
        DisposeInternal().SynchronousWait();
    }
    public async ValueTask DisposeAsync()
    {
        await DisposeInternal().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    public async void Dispose()
    {
        await DisposeInternal().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    private async Task DisposeInternal()
    {
        if (0 != Interlocked.Exchange(ref _isDisposed, 1))
            return;
        try
        {
            await DisposeAsyncCore().ConfigureAwait(false);
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
    protected virtual ValueTask DisposeAsyncCore()
    {
# if NET8_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask();
#endif
    }
}
