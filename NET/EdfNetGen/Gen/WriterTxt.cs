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

    private EdfErr WriteValue(in byte[] val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (CurrentSchema?.Type.Type != PoType.Char)
            return EdfErr.WrongType;
        int len = (int)CurrentSchema.Type.GetTotalElements();
        var formatter = new CharArrayFormatter(len);
        var writer = new BufWriterTxt(_state, CurrentSchema?.Type);
        writer.RecBegin();
        formatter.Serialize(ref writer, val, _options);
        writer.RecEnd();
        writer.EnsureEmpty();
        return EdfErr.IsOk;
    }
    public override EdfErr WriteValue<T>(in T val)
    {
        if (CurrentSchema?.Type.Type == PoType.Char && val is byte[] chArr)
            return WriteValue(chArr);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var writer = new BufWriterTxt(_state, CurrentSchema?.Type);
        writer.RecBegin();
        formatter.Serialize(ref writer, val, _options);
        writer.RecEnd();
        writer.EnsureEmpty();
        return EdfErr.IsOk;
    }
}
