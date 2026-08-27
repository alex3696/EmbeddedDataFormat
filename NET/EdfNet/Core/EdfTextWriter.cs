using EdfNet.Core.Text;

namespace EdfNet.Core;

public class EdfTextWriter : BaseDisposable, IWriter
{
    protected readonly TextCircularEdfTypeEnumerator _enum = new();
    protected readonly EdfFormatterOptions _options = EdfFormatterOptions.Default;
    protected readonly BufferedTextWriter _textWriter;

    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;

    public EdfTextWriter(Stream stream, EdfConfig? cfg = null)
    {
        Cfg = cfg ?? EdfConfig.Default;
        _textWriter = new BufferedTextWriter(stream, 1024);
        if (0 == stream.Position)
            WriteConfig(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        base.Dispose(disposing);
    }

    public void WriteConfig(EdfConfig h)
    {
        _textWriter.WriteConfig(h);
        CurrentSchema = null;
    }
    public void WriteSchema(EdfSchema? sch)
    {
        CurrentSchema = sch;
        if (sch != null)
        {
            _textWriter.WriteSchema(sch);
            _enum.Reset(sch.Type);
        }
    }
    public EdfErrorCode WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        IFormatter<T> formatter = EdfFormatterProvider<T>.Formatter;
        EdfFormatterNotRegistredException.ThrowIfNull(formatter);
        var writer = new BufWriterTxt(_textWriter, _enum);
        formatter.Serialize(ref writer, val, _options);
        return _enum.PrimOffset == 0 ? EdfErrorCode.IsOk : EdfErrorCode.SrcDataRequred;
    }
    public EdfErrorCode WriteInfData<T>(ushort id, EdfPrimitiveType pt, string name, T d)
    {
        WriteSchema(new EdfSchema() { Id = id, Type = new(pt), Name = name, });
        return WriteValue(d);
    }
    public void Flush() => _textWriter.Flush();

}
