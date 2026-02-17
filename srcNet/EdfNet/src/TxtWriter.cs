namespace NetEdf.src;

public class TxtWriter : BaseWriter
{
    readonly Stream _st;

    public TxtWriter(Stream stream, Header? cfg = null)
        : base(cfg ?? Header.Default)
    {
        _st = stream;
        SepBeginStruct = "{"u8.ToArray();
        SepEndStruct = "}"u8.ToArray();
        SepBeginArray = "["u8.ToArray();
        SepEndArray = "]"u8.ToArray();
        SepVarEnd = ";"u8.ToArray();
        SepRecBegin = "\n<= "u8.ToArray();
        SepRecEnd = ">"u8.ToArray();
        if(0 == stream.Position)
            Write(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _st.Flush();
        base.Dispose(disposing);
    }
    public override void Flush()
    {
        _st.Write(_blkData.AsSpan(0, _blkQty));
        _blkQty = 0;
    }
    protected void Write(string? str)
    {
        if (!string.IsNullOrEmpty(str))
            _st.Write(Encoding.UTF8.GetBytes(str));
    }
    protected static string GetOffset(int noffset)
    {
        string offset = "";
        for (int i = 0; i < noffset; i++)
            offset += "  ";
        return offset;
    }
    public override void Write(Header h)
    {
        Flush();
        Write($"<~ {{version={h.VersMajor}.{h.VersMinor}; bs={h.Blocksize}; encoding={h.Encoding}; flags={(uint)h.Flags}; }} >\n");
        //Write($"// ? - struct @ - data // - comment");
        _currDataType = null;
        _blkQty = 0;
    }
    public override void Write(TypeRec t)
    {
        Flush();
        Write($"\n\n<? {{");
        Write($"{t.Id};\"{t.Name}\"");
        if (!string.IsNullOrEmpty(t.Desc))
            Write($";\"{t.Desc}\"");
        Write($"}} ");
        ToString(t.Inf);
        Write($">");
        _currDataType = t.Inf;
        _blkQty = 0;
    }
    protected void ToString(TypeInf t, int noffset = 0)
    {
        string offset = GetOffset(noffset);
        Write(offset);
        Write(t.Type.ToString());
        if (null != t.Dims)
        {
            foreach (var d in t.Dims)
                Write($"[{d}]");
        }
        if (!string.IsNullOrEmpty(t.Name))
            Write($" \"{t.Name}\"");
        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            Write($"\n{offset}{{");
            foreach (var it in t.Childs)
            {
                Write($"\n");
                ToString(it, noffset + 1);
            }
            Write($"\n{offset}}}");
        }
        else
            Write(";");
    }

    protected override EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w)
        => Primitives.TrySrcToTxt(t, obj, dst, out w);
    protected override EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
    {
        if (0 < skip)
        {
            skip--;
            return EdfErr.IsOk;
        }
        if (0 == src.Length)
        {
            wqty++;
            return EdfErr.IsOk;
        }
        if (src.Length > dst.Length)
            return EdfErr.DstBufOverflow;
        src.CopyTo(dst);
        wqty++;
        writed += src.Length;
        dst = dst.Slice(src.Length);
        return EdfErr.IsOk;
    }

}
