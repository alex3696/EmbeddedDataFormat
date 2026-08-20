namespace EdfNet.Core;

public sealed class CircularEnumaratorEdfType
{
    public int PrimOffset = 0;
    public uint RecordId = 0;
    private EdfType? _rootType;
    private readonly StrongBox<EdfTypeEnumeratorStackInlineArray> _enm = new();
    public void Reset(EdfType rootType)
    {
        RecordId = 0;
        _rootType = rootType;
        Restart();
    }
    private bool Restart()
    {
        PrimOffset = 0;
        _enm.Value.Reset(_rootType);
        return _rootType != null;
    }
    public bool MoveNext()
    {
        bool result = _enm.Value.MoveNext();
        if (result)
        {
            PrimOffset++;
            if (_enm.Value.IsEmpty)
            {
                RecordId++;
                if (!Restart())
                    return false;
                result = _enm.Value.MoveNext();
            }
        }
        return result;
    }
    public EdfType? GetCurrentType()
    {
        if (null == _enm.Value.Current) // start enumerate
        {
            if (MoveNext())
                return _enm.Value.Current;
            throw new EdfWrongTypeException();
        }
        return _enm.Value.Current;
    }
}

public sealed class CircularEnumaratorEdfTypeTxt
{
    public int PrimOffset = 0;
    public uint RecordId = 0;
    private EdfType? _rootType;
    private EdfTypeEnumeratorToken _enm = new();
    public void Reset(EdfType rootType)
    {
        RecordId = 0;
        _rootType = rootType;
        Restart();
    }
    private bool Restart()
    {
        if(null == _rootType)
            return false;
        PrimOffset = 0;
        _enm.Reset(_rootType);
        return _enm.MoveNext();
    }
    public bool MoveNext()
    {
        bool result = _enm.MoveNext();
        if (result)
        {
            PrimOffset++;
        }
        else
        {
            if (_enm.IsEmpty)
            {
                RecordId++;
                return Restart();
            }
        }
        return result;
    }
    public Token CurrentToken => _enm.CurrentToken;
    public EdfType? GetCurrentType()
    {
        if (null == _enm.Current) // start enumerate
        {
            if (MoveNext())
                return _enm.Current;
            throw new EdfWrongTypeException();
        }
        return _enm.Current;
    }
}
