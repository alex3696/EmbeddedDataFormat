using EdfNet.Interfaces;

namespace EdfNet.Ref;

public class BinReader : BaseReaderBin
{
    private readonly RecursiveReaderBin _reader;
    public BinReader(Stream stream, Config? cfg = default)
        :base(stream, cfg)
    {
        _reader = new(_blkData, _stream);
    }

    public EdfErr TryReadNew<T>(out T? ret)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        EdfErr err = _reader.DoRead(CurrentSchema.Type, out var obj, typeof(T));
        ret = (T)obj;
        return err;
    }


    public static EdfErr ReadObject(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, totalElement, ref skip, ref qty, ref readed, ref ret);
        return ReadElement(t, src, ref skip, ref qty, ref readed, ref ret);
    }
    public static EdfErr ReadElement(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, ref skip, ref qty, ref readed, ref ret);
        return ReadPrimitive(t, src, ref skip, ref qty, ref readed, ref ret);
    }
    static EdfErr ReadArray(EdfType t, ReadOnlySpan<byte> src, uint totalElement, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        Type csType = ret.GetType();
        if (!csType.IsArray)
            throw new ArrayTypeMismatchException();
        var elementType = csType.GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        var arr = ret as Array;
        ArgumentNullException.ThrowIfNull(arr);
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadElement(t, src, elementType, ref skip, ref qty, ref readed, out var arrItem)))
                return err;
            if (0 < readed)
            {
                arr.SetValue(arrItem, i);
                src = src.Slice(readed - r);
            }
        }
        return err;
    }
    static EdfErr ReadStruct(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        Type csType = ret.GetType();
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
        int fieldId = 0;
        foreach (var child in t.Childs)
        {
            var r = readed;
            var field = fields[fieldId++];
            if (EdfErr.IsOk != (err = ReadObject(child, src, field.PropertyType, ref skip, ref qty, ref readed, out var childVal)))
                return err;
            field.SetValue(ret, childVal);
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadPrimitive(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (0 < skip)
        {
            skip--;
            return EdfErr.IsOk;
        }
        EdfErr err;
        if (0 != (err = Primitives.TryBinToSrc(t, src, out var r, out var result)))
            return err;
        if (result is null)
            return EdfErr.WrongType;
        ret = result;
        readed += r;
        qty++;
        return err;
    }

    public static EdfErr ReadObject(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, csType, totalElement, ref skip, ref qty, ref readed, out ret);
        return ReadElement(t, src, csType, ref skip, ref qty, ref readed, out ret);
    }
    public static EdfErr ReadElement(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, csType, ref skip, ref qty, ref readed, out ret);
        return ReadPrimitive(t, src, csType, ref skip, ref qty, ref readed, out ret);
    }
    static EdfErr ReadArray(EdfType t, ReadOnlySpan<byte> src, Type csType, uint totalElement, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (!csType.IsArray)
            throw new ArrayTypeMismatchException();
        var elementType = csType.GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        var arr = Array.CreateInstance(elementType, totalElement);
        ret = arr;
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadElement(t, src, elementType, ref skip, ref qty, ref readed, out var arrItem)))
                return err;
            arr.SetValue(arrItem, i);
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadStruct(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        ret = default;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        ret = Activator.CreateInstance(csType);
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
        int fieldId = 0;
        foreach (var child in t.Childs)
        {
            var r = readed;
            var field = fields[fieldId++];
            if (EdfErr.IsOk != (err = ReadObject(child, src, field.PropertyType, ref skip, ref qty, ref readed, out var childVal)))
                return err;
            field.SetValue(ret, childVal);
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadPrimitive(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (0 < skip)
        {
            skip--;
            ret = null;
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t, src, out var r, out ret)))
            return err;
        readed += r;
        qty++;
        return err;
    }


    int _skip = 0;
    int _readed = 0;
    object? _ret;
    public EdfErr TryRead<T>(out T? ret)
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        EdfErr err;
        ret = default;
        ReadOnlySpan<byte> src = _blkData.CurrentData.Slice(_readed, _blkData.DataLen - _readed);
        do
        {
            int qty = 0;
            int skip = _skip;
            int readed = 0;

            if (null != _ret)
                err = ReadObject(CurrentSchema.Type, src, ref skip, ref qty, ref readed, ref _ret);
            else
                err = ReadObject(CurrentSchema.Type, src, typeof(T), ref skip, ref qty, ref readed, out _ret);
            src = src.Slice(readed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.DstBufOverflow: return err;
                case EdfErr.SrcDataRequred:
                    _skip += qty;
                    _readed = 0;
                    break;
                case EdfErr.IsOk:
                    ret = (T?)Convert.ChangeType(_ret, typeof(T));
                    _readed += readed;
                    _skip = 0;
                    _ret = null;
                    return err;
            }
        }
        while (err != EdfErr.SrcDataRequred);
        return err;
    }


}
