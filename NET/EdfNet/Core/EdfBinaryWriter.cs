using EdfNet.Core.Binary;

namespace EdfNet.Core;

public class EdfBinaryWriter : BaseWriterBin
{
    protected readonly BufStateBin _state;
    protected readonly EdfFormatterOptions _options = EdfFormatterOptions.Default;

    public EdfBinaryWriter(Stream stream, EdfConfig? cfg = default)
        : base(stream, cfg)
    {
        _state = new(_stream, _blk);
    }
    protected override void Dispose(bool disposing)
    {
        _state?.Dispose();
        base.Dispose(disposing);
    }

    public override void WriteSchema(EdfSchema sch)
    {
        base.WriteSchema(sch);
        _state.Enum.Reset(sch.Type);
    }
    public override EdfErrorCode WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfFormatterProvider<T>.Formatter;
        EdfFormatterNotRegistredException.ThrowIfNull(formatter);
        var writer = new BufWriterBin(_state);
        formatter.Serialize(ref writer, val, _options);
        return _state.Enum.PrimOffset == 0 ? EdfErrorCode.IsOk : EdfErrorCode.SrcDataRequred;
    }
}
