namespace EdfNet.Gen;

public class WriterTxt : BaseWriterTxt
{
    private readonly BufStateTxt _state;
    private readonly EdfOptions _options = EdfOptions.Default;

    public WriterTxt(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _state = new BufStateTxt(stream, new byte[cfg?.Blocksize ?? Config.Default.Blocksize]);
    }

    public override EdfErr WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var writer = new BufWriterTxt(_state);
        formatter.Serialize(ref writer, val, _options);
        return EdfErr.IsOk;
    }
}
