namespace EdfNet.Core;

public sealed class CircularEnumaratorEdfType
{
    public int PrimOffset = 0;
    public uint RecordId = 0;
    private EdfType? _rootType;
    private EdfTypeEnumeratorStackInlineArray _enm = new();
    public void Reset(EdfType rootType)
    {
        RecordId = 0;
        _rootType = rootType;
        Restart();
    }
    private bool Restart()
    {
        if (null == _rootType)
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
        if (null == _rootType)
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
