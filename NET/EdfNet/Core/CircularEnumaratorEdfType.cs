namespace EdfNet.Core;

public sealed class CircularEnumaratorEdfType
{
    public int PrimOffset = 0;
    public uint RecordId = 0;
    private EdfTypeEnumeratorStackInlineArray _enm = new();
    public void Reset(EdfType rootType)
    {
        RecordId = 0;
        PrimOffset = 0;
        _enm.Reset(rootType);
        _enm.MoveNext();
    }
    public bool MoveNext()
    {
        if (_enm.MoveNext())
        {
            PrimOffset++;
            return true;
        }
        if (_enm.IsEnded)
        {
            RecordId++;
            PrimOffset = 0;
            if (_enm.Restart())
                return _enm.MoveNext();
        }
        return false;
    }
    public EdfType CurrentType => _enm.Current;
}

public sealed class CircularEnumaratorEdfTypeTxt
{
    public int PrimOffset = 0;
    public uint RecordId = 0;
    private EdfTypeEnumeratorToken _enm = new();
    public void Reset(EdfType rootType)
    {
        RecordId = 0;
        PrimOffset = 0;
        _enm.Reset(rootType);
        _enm.MoveNext();
    }
    public bool MoveNext()
    {
        if (_enm.MoveNext())
        {
            PrimOffset++;
            return true;
        }
        if (_enm.IsEnded)
        {
            RecordId++;
            PrimOffset = 0;
            if (_enm.Restart())
                return _enm.MoveNext();
        }
        return false;
    }
    public EdfType CurrentType => _enm.Current;
    public Token CurrentToken => _enm.CurrentToken;
}
