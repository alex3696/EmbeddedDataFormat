using EdfNet.Core.Binary;

namespace EdfNet.Core;

public class WriterBin : BaseWriterBin
{
    protected readonly BufStateBin _state;
    protected readonly Interfaces.EdfOptions _options = Interfaces.EdfOptions.Default;

    public WriterBin(Stream stream, EdfConfig? cfg = default)
        : base(stream, cfg)
    {
        _state = new(_stream, _blkData);
    }

    public override void WriteSchema(EdfSchema sch)
    {
        base.WriteSchema(sch);
        _state.Enum.Reset(sch.Type);
    }
    public override EdfErr WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var writer = new BufWriterBin(_state);
        formatter.Serialize(ref writer, val, _options);
        return _state.Enum.PrimOffset == 0 ? EdfErr.IsOk : EdfErr.SrcDataRequred;
    }
}
