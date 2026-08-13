namespace EdfNet.Gen;

public class WriterBin2 : BaseWriterBin
{
    public WriterBin2(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _rstate = new(_stream, _blkData);
    }

    public override void Write(Schema sch)
    {
        base.Write(sch);
        _rstate.Skip = 0;
        _rstate.RecordId = 0;
        _rstate.PrimOffset = 0;
    }
    private readonly BufWriterState _rstate;
    private readonly EdfOptions _options = EdfOptions.Default;

    public new EdfErr WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var writer = new BufWriterBin(_rstate);
        formatter.Serialize(ref writer, val, _options);
        return EdfErr.IsOk;
    }

    public override EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
    {
        throw new NotImplementedException();
    }
}
