using NetEdf.Base;

namespace NetEdf.src;

public abstract class BaseWriter : BaseDisposable
{
    protected readonly Header _cfg;
    protected TypeInf? _currDataType;

    public BaseWriter(Header header) => _cfg = header;
    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    public abstract void Write(Header v);
    public abstract void Write(TypeRec t);
    public abstract int Write(TypeInf t, object obj);
    public abstract void Flush();

}

