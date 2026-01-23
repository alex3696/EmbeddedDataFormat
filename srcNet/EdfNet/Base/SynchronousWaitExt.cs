namespace NetEdf.Base;
// https://habr.com/ru/articles/721050/
public static class SynchronousWaitExt
{
    public static void SynchronousWait(this Task t)
    {
        if (SynchronizationContext.Current == null && TaskScheduler.Current == TaskScheduler.Default)
            t.GetAwaiter().GetResult();
        else
            Task.Run(() => t).GetAwaiter().GetResult();
    }
    public static T SynchronousWait<T>(this Task<T> t)
    {
        if (SynchronizationContext.Current == null && TaskScheduler.Current == TaskScheduler.Default)
            return t.GetAwaiter().GetResult();
        return Task.Run(() => t).GetAwaiter().GetResult();
    }
}
