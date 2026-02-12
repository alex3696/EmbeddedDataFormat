using NetEdf.Base;

namespace NetEdf.src;

public abstract class BaseWriter : BaseDisposable
{
    public readonly Header Cfg;
    protected TypeInf? _currDataType;

    public TypeInf? CurrDataType => _currDataType;

    public BaseWriter(Header header) => Cfg = header;
    //protected override void Dispose(bool disposing) => base.Dispose(disposing);

    public abstract void Write(Header v);
    public abstract void Write(TypeRec t);
    public abstract int Write(object obj);
    public abstract void Flush();

}
public abstract class BaseReader : BaseDisposable
{

}


public static class BaseWriterExt
{
    public static void WriteInfData(this BaseWriter dw, UInt32 id, PoType pt, string name, object d)
    {
        dw.Write(new TypeRec() { Id = id, Inf = new(pt), Name = name, });
        ArgumentNullException.ThrowIfNull(dw.CurrDataType);
        dw.Write(d);
    }
}


