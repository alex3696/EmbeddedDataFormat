namespace NetEdf.src;

public class MultiWriter : IDfWriter
{
    List<BaseBlockWriter> _w;

    public MultiWriter(params BaseBlockWriter[] w)
    {
        _w = w.ToList();
    }
    public void Flush()
    {
        foreach (var w in _w)
            w.Flush();
    }
    public void Write(Header v)
    {
        foreach (var w in _w)
            w.Write(v);
    }
    public void WriteVarData(ReadOnlySpan<byte> b)
    {
        foreach (var w in _w)
            w.WriteVarData(b);
    }
    public void WriteVarInfo(TypeInf t)
    {
        foreach (var w in _w)
            w.WriteVarInfo(t);
    }
}
