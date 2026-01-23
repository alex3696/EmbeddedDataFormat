using NetEdf.Base;

namespace NetEdf.src;

public interface IDfWriter
{
    void Write(Header v);
    void WriteVarInfo(TypeInfo t);
    void WriteVarData(ReadOnlySpan<byte> b);
    void Flush();
}

public static class IDfWriterExt
{
    public static void WriteVarData<T>(this IDfWriter t, T val, bool flush = true)
        where T : struct
    {
        t.WriteVarData(StructSerialize.ToBytes(val));
    }
    public static void Write(this IDfWriter t, Var v)
    {
        if (null == v.Info)
            return;
        // Values
        t.WriteVarInfo(v.Info);
        if (null != v.Values)
            for (var i = 0; i < v.Values.Count; i++)
                t.WriteVarData(v.Values[i]);
    }
}
