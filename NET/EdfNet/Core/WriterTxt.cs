using EdfNet.Core.Text;

namespace EdfNet.Core;

public class WriterTxt : BaseWriterTxt
{
    protected readonly BufStateTxt _state;
    protected readonly Interfaces.EdfOptions _options = Interfaces.EdfOptions.Default;

    public WriterTxt(Stream stream, EdfConfig? cfg = default)
        : base(stream, cfg)
    {
        _state = new BufStateTxt(stream, new byte[cfg?.BlockSize ?? EdfConfig.Default.BlockSize]);
    }
    public override void WriteSchema(EdfSchema? sch)
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
