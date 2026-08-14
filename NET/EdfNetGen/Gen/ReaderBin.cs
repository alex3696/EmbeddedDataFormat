namespace EdfNet.Gen;

public class ReaderBin : BaseReaderBin
{
    private BufStateBin _state;
    private readonly EdfOptions _options = EdfOptions.Default;
    public ReaderBin(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _state = new BufStateBin(stream, _blkData);
    }

    public T ReadValue<T>()
    {
        //ObjectDisposedException.ThrowIf(IsDisposed, this);
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var reader = new BufReaderBin(_state);
        return formatter.Deserialize(ref reader, _options);
    }
}
