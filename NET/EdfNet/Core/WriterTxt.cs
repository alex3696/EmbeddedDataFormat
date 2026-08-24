namespace EdfNet.Core;

public class WriterTxt : BaseWriterTxt
{
    protected readonly BufStateTxt _state;
    protected readonly EdfOptions _options = EdfOptions.Default;

    public WriterTxt(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _state = new BufStateTxt(stream, new byte[cfg?.Blocksize ?? Config.Default.Blocksize]);
    }
    public override void WriteSchema(Schema? sch)
    {
        base.WriteSchema(sch);
        if (sch != null)
            _state.Enum.Reset(sch.Type);
    }
    public override EdfErr WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var writer = new BufWriterTxt(_state);
        formatter.Serialize(ref writer, val, _options);
        return _state.Enum.PrimOffset == 0 ? EdfErr.IsOk : EdfErr.SrcDataRequred;
    }
}
