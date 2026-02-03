namespace NetEdf.src;

public class TxtWriter : BaseBlockWriter
{
    readonly Stream _stream;
    readonly StructWriter _tw;
    readonly byte[] _dstBuff = new byte[256];

    public TxtWriter(Stream stream, Header cfg, StructWriter.WritePrimitivesFn fn)
        : base(cfg)
    {
        _stream = stream;
        _tw = new StructWriter(fn ?? Primitives.BinToStr)
        {
            SepBeginStruct = "{"u8.ToArray(),
            SepEndStruct = "}"u8.ToArray(),
            SepBeginArray = "["u8.ToArray(),
            SepEndArray = "]"u8.ToArray(),
            SepVar = ";"u8.ToArray(),
            SepRecBegin = "\n= "u8.ToArray(),
            SepRecEnd = ""u8.ToArray(),
        };
        Write(cfg);
    }

    protected void Write(string str)
    {
        _stream.Write(Encoding.UTF8.GetBytes(str));
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _stream.Flush();
        base.Dispose(disposing);
    }
    public override void Write(Header h)
    {
        Write($"~ ");
        Write($"version={h.VersMajor}.{h.VersMinor} bs = {h.Blocksize} encoding={h.Encoding} flags={h.Flags} \n");
        Write($"// ? - struct @ - data // - comment");
    }
    public override void WriteVarInfo(TypeInf t)
    {
        Write($"\n\n? ");
        Write(TypeInf.ToString(t).TrimStart('\n'));
        _currDataType = t;
        _tw.Clear();
    }
    public override void WriteVarData(ReadOnlySpan<byte> src)
    {
        if (null != _currDataType && 0 < src.Length)
        {
            int wr, r, w;
            do
            {
                Span<byte> dst = _dstBuff;
                wr = _tw.WriteMultipleValues(_currDataType, src, dst, out r, out w);
                src = src.Slice(r);
                _stream.Write(_dstBuff.AsSpan(0, w));
            }
            while (0 <= wr /*&& 0 < src.Length*/);
        }
    }
    public override void Flush()
    {
        _stream.Flush();
        _tw.Clear();
    }
}
