namespace EdfNet.Core;

public static class EdfTypeFlatExt
{
    //[DebuggerDisplay("{DebugString(),nq}")]
    //public struct FlatItem(PoType po)
    //{
    //    public int CharLen = 0;
    //    public PoType Type = po;
    //    public readonly string DebugString() => $"Type: {Type}, " +
    //        (PoType.Char == Type ? $"CharLen:{CharLen}" : string.Empty);
    //}

    public static ReadOnlySpan<EdfType> GetFlatRecursive(this EdfType et, Span<EdfType> dst)
    {
        int index = 0;
        GetFlatRecursive(et, dst, ref index);
        return dst.Slice(0, index);
    }
    private static void GetFlatRecursive(EdfType et, Span<EdfType> items, ref int index)
    {
        var total = et.GetTotalElements();
        if (PoType.Struct == et.Type && null != et.Childs && 0 < et.Childs.Length)
        {
            for (int i = 0; i < total; i++)
                for (int f = 0; f < et.Childs.Length; f++)
                    GetFlatRecursive(et.Childs[f], items, ref index);
        }
        else
        {
            for (int i = 0; i < total; i++)
            {
                items[index++] = et;
            }
        }
    }

    public static ReadOnlySpan<EdfType> GetFlatEnumerable(this EdfType et, Span<EdfType> dst)
    {
        int index = 0;
        foreach (var item in et.GetFlatEnumerable())
            dst[index++] = item;
        return dst.Slice(0, index);
    }
    public static IEnumerable<EdfType> GetFlatEnumerable(this EdfType et)
    {
        uint total = et.GetTotalElements();
        if (et.Type == PoType.Struct && et.Childs != null && et.Childs.Length > 0)
        {
            for (int i = 0; i < total; i++)
            {
                for (int f = 0; f < et.Childs.Length; f++)
                    foreach (var item in et.Childs[f].GetFlatEnumerable())
                        yield return item;
            }
        }
        else
        {
            for (uint i = 0; i < total; i++)
                yield return et;
        }
    }
}
