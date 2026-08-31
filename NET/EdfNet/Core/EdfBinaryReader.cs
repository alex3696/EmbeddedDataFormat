using EdfNet.Core.Binary;

namespace EdfNet.Core;

public class EdfBinaryReader : BaseReaderBin
{
    protected readonly BufStateBin _state;
    protected readonly Interfaces.EdfFormatterOptions _options = Interfaces.EdfFormatterOptions.Default;

    public EdfBinaryReader(Stream stream, EdfConfig? cfg = default)
        : base(stream, cfg)
    {
        _state = new BufStateBin(stream, _blk);
    }
    protected override void Dispose(bool disposing)
    {
        _state.Dispose();
        base.Dispose(disposing);
    }
    protected override void OnSchemaBlockRead()
    {
        if (CurrentSchema?.Type != null)
            _state.Enum.Reset(CurrentSchema.Type);
    }
    protected override void OnReadDatBlockStart()
    {
        base.OnReadDatBlockStart();
        _state.Readed = 0;
    }

    public int DataAvailable => _state.ReadAvailableLen;

    public T ReadValue<T>()
    {
        //ObjectDisposedException.ThrowIf(IsDisposed, this);
        IFormatter<T> formatter = EdfFormatterProvider<T>.Formatter;
        EdfFormatterNotRegistredException.ThrowIfNull(formatter);
        var reader = new BufReaderBin(_state);
        return formatter.Deserialize(ref reader, _options);
    }
}
