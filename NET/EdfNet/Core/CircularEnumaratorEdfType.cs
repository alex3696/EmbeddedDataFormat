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

