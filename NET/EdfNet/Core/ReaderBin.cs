using EdfNet.Core.Binary;

namespace EdfNet.Core;

public class ReaderBin : BaseReaderBin
{
    protected readonly BufStateBin _state;
    protected readonly Interfaces.EdfOptions _options = Interfaces.EdfOptions.Default;

    public ReaderBin(Stream stream, EdfConfig? cfg = default)
        : base(stream, cfg)
    {
        _state = new BufStateBin(stream, _blkData);
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
