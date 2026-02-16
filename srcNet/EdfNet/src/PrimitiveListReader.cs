namespace NetEdf.src;

// заменить на IEnumerable<object>
public static class PrimitiveListReader
{
    public static EdfErr ReadObjects(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, totalElement, ref skip, ref qty, ref readed, ret);
        return ReadElement(t, src, ref skip, ref qty, ref readed, ret);
    }
    static EdfErr ReadElement(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, ref skip, ref qty, ref readed, ret);
        return ReadPrimitive(t, src, ref skip, ref qty, ref readed, ret);
    }
    static EdfErr ReadArray(TypeInf t, ReadOnlySpan<byte> src, uint totalElement, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        EdfErr err = EdfErr.IsOk;
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadElement(t, src, ref skip, ref qty, ref readed, ret)))
                return err;
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadStruct(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        foreach (var child in t.Childs)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadObjects(child, src, ref skip, ref qty, ref readed, ret)))
                return err;
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadPrimitive(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        if (0 < skip)
        {
            skip--;
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t.Type, src, out var r, out var retVal)))
            return err;
        if(null != retVal)
            ret.Add(retVal);
        readed += r;
        qty++;
        return err;
    }
}
