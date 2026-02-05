namespace NetEdf.src;

public class TxtWriter : BaseWriter
{
    readonly Stream _stream;
    readonly byte[] _dstBuff = new byte[256];

    public TxtWriter(Stream stream, Header cfg)
        : base(cfg)
    {
        _stream = stream;
        //_tw = new StructWriter(fn ?? Primitives.BinToStr)
        //{
        //    SepBeginStruct = "{"u8.ToArray(),
        //    SepEndStruct = "}"u8.ToArray(),
        //    SepBeginArray = "["u8.ToArray(),
        //    SepEndArray = "]"u8.ToArray(),
        //    SepVar = ";"u8.ToArray(),
        //    SepRecBegin = "\n= "u8.ToArray(),
        //    SepRecEnd = ""u8.ToArray(),
        //};
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
    public override void Write(TypeRec t)
    {
        /*
        Write($"\n\n? ");
        Write(TypeInf.ToString(t).TrimStart('\n'));
        _currDataType = t;
        _tw.Clear();
        */
    }
    public override int Write(TypeInf t, object obj)
    {
    /*
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
            while (0 <= wr );
        }
    */
        return 0;
    }
    public override void Flush()
    {
        _stream.Flush();
        //_tw.Clear();
    }
}
