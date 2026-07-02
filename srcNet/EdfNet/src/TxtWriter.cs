namespace NetEdf.src;

public class TxtWriter : BaseWriter
{
    readonly Stream _st;
    private readonly byte[] _buf;
    protected override ushort _DataLen { get; set; }
    protected override Span<byte> _DataBuffer => _buf;


    public TxtWriter(Stream stream, Config? cfg = null)
        : base(cfg ?? Config.Default)
    {
        _st = stream;
        _buf = new byte[Cfg.Blocksize];

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
        _st.Write(_DataBuffer.Slice(0, _DataLen));
        _DataLen = 0;
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
    public override void Write(Config h)
    {
        Flush();
        Write($"<~ {{version={h.VersMajor}.{h.VersMinor}; bs={h.Blocksize}; encoding={h.Encoding}; flags={(uint)h.Flags}; }} >\n");
        //Write($"// ? - struct @ - data // - comment");
        CurrentSchema = null;
        _DataLen = 0;
    }
    public override void Write(Schema sch)
    {
        Flush();
        Write($"\n\n<? {{");
        Write($"{sch.Id};\"{sch.Name}\"");
        if (!string.IsNullOrEmpty(sch.Desc))
            Write($";\"{sch.Desc}\"");
        Write($"}} ");
        ToString(sch.Type);
        Write($">");
        CurrentSchema = sch;
        _DataLen = 0;
    }
    protected void ToString(EdfType t, int noffset = 0)
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
