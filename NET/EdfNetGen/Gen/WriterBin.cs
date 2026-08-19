namespace EdfNet.Gen;

public class WriterBin : BaseWriterBin
{
    public WriterBin(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _state = new(_stream, _blkData);
    }

    public override void WriteSchema(Schema sch)
    {
        base.WriteSchema(sch);
        _state.Enum.Reset(sch.Type);
    }
    private readonly BufWriterState _state;
    private readonly EdfOptions _options = EdfOptions.Default;

    public override EdfErr WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var writer = new BufWriterBin(_state);
        writer.RecBegin();
        formatter.Serialize(ref writer, val, _options);
        writer.RecEnd();
        return EdfErr.IsOk;
    }
}
