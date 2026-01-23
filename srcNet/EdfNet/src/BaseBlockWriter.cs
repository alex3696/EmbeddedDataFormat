using NetEdf.Base;

namespace NetEdf.src;

public abstract class BaseBlockWriter : BaseDisposable, IDfWriter
{
    protected readonly Header _cfg;
    protected TypeInfo? _currDataType;

    public BaseBlockWriter(Header header)
    {
        _cfg = header;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public abstract void Write(Header v);
    public abstract void WriteVarInfo(TypeInfo t);
    public abstract void WriteVarData(ReadOnlySpan<byte> b);
    public abstract void Flush();

}

