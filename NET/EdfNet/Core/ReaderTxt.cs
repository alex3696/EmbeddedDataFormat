using System.Text.RegularExpressions;

namespace EdfNet.Core;

public partial class ReaderTxt //: BaseReaderBin
{
    private static readonly Regex _rxBlock = BlockRegex();
    private static readonly Regex _rxConfig = ConfigRegex();

    protected readonly Config _cfg;
    protected readonly byte[] _buf;
    protected readonly BufStateTxt _state;
    protected readonly EdfOptions _options = EdfOptions.Default;

    public Config Cfg => _cfg;
    public Schema? CurrentSchema;

    public ReaderTxt(Stream stream, Config? cfg = default)
    //: base(stream, cfg)
    {
        _cfg = cfg ?? Config.Default;
        var bufLen = _cfg.Blocksize;
        _buf = new byte[bufLen];
        _state = new BufStateTxt(stream, _buf);
    }
    public bool ReadBlock()
    {
        // read block from stream < >
        return true;
    }
    public BlockType GetBlockType() => BlockType.Data;

    public T ReadValue<T>()
    {
        //ObjectDisposedException.ThrowIf(IsDisposed, this);
        IFormatter<T> formatter = EdfProvider<T>.Formatter;
        if (formatter == null)
            throw new InvalidOperationException($"Тип {typeof(T).FullName} не зарегистрирован в системе сериализации.");
        var reader = new BufReaderTxt(_state);
        return formatter.Deserialize(ref reader, _options);
    }

    [GeneratedRegex(@"<(?<prefix>[~|?|=])\s*(?<content>[\s\S]*?)>(?![^<]*>)"
        , RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex BlockRegex();


    [GeneratedRegex(@"version=(?<major>\d).(?<minor>\d).*bs=(?<bs>\d+).*encoding=(?<encoding>\d+).*flags=(?<flags>\d+)"
        , RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex ConfigRegex();
}
