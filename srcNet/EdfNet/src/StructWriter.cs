namespace NetEdf.src;


public class StructWriter
{
    protected int _skip = 0;
    public void Clear()
    {
        _skip = 0;
        _srcLen = 0;
    }
    public int Skip => _skip;

    readonly byte[] _srcBuf = new byte[256];
    int _srcLen = 0;

    public int WriteMultipleValues(TypeInfo inf, ReadOnlySpan<byte> xsrc, Span<byte> dst, out int readed, out int writed)
    {
        readed = writed = 0;
        int wr;
        ReadOnlySpan<byte> src;
        do
        {
            if (0 < _srcLen)
            {
                int len = int.Min(_srcBuf.Length - _srcLen, xsrc.Length);
                if (0 < len)
                {
                    xsrc.Slice(0, len).CopyTo(_srcBuf.AsSpan(_srcLen, len));
                    xsrc = xsrc.Slice(len);
                    _srcLen += len;
                    readed += len;
                    // add copy
                }
                src = _srcBuf.AsSpan(0, _srcLen);
            }
            else
            {
                src = xsrc;
                xsrc = xsrc.Slice(0, 0);
                readed += src.Length;
            }

            wr = WriteSingleValue(inf, src, dst, out var r, out var w);
            //readed += r;
            writed += w;
            src = src.Slice(r);
            dst = dst.Slice(w);

            src.CopyTo(_srcBuf);
            _srcLen = src.Length;
        }
        while (0 == wr && 0 < src.Length);
        return wr;
    }
    public int WriteSingleValue(TypeInfo inf, ReadOnlySpan<byte> src, Span<byte> dst, out int readed, out int writed)
    {
        int wqty = 0;
        readed = writed = 0;
        if (0 == src.Length)
            return -1;
        if (0 == _skip)
            WriteSep(SepRecBegin, ref dst, ref writed);
        int wr = WriteData(inf, ref src, ref dst, ref _skip, ref wqty, ref readed, ref writed);
        if (0 == wr)
        {
            WriteSep(SepRecEnd, ref dst, ref writed);
            _skip = 0;
        }
        else
        {
            _skip = wqty;
        }
        return wr;
    }
    protected int WriteData(TypeInfo inf, ref ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int readed, ref int writed)
    {
        uint totalElement = 1;
        for (int i = 0; i < inf.Dims?.Length; i++)
            totalElement *= inf.Dims[i];

        if (PoType.Char == inf.Type)
        {
            if (src.Length < totalElement)
                return -1;
            if (dst.Length < totalElement)
                return 1;
            if (skip >= totalElement)
            {
                skip -= (int)totalElement;
                return 0;
            }
            int wr = _writePrimitives(inf.Type, src.Slice(0, (int)totalElement), dst, out int r, out int w);
            if (0 == wr)
            {
                wqty += (int)totalElement;
                readed += r;
                writed += w;
                src = src.Slice(r);
                dst = dst.Slice(w);
                WriteSep(SepVar, ref dst, ref writed);
            }
            return wr;
        }

        if (0 == skip && 1 < totalElement)
            WriteSep(SepBeginArray, ref dst, ref writed);
        for (int i = 0; i < totalElement; i++)
        {
            if (PoType.Struct == inf.Type)
            {
                if (inf.Items != null && 0 != inf.Items.Length)
                {
                    if (0 == skip)
                        WriteSep(SepBeginStruct, ref dst, ref writed);
                    foreach (var s in inf.Items)
                    {
                        var wr = WriteData(s, ref src, ref dst, ref skip, ref wqty, ref readed, ref writed);
                        if (0 != wr)
                            return wr;
                    }
                    if (0 == skip)
                        WriteSep(SepEndStruct, ref dst, ref writed);
                }
            }
            else
            {
                if (0 < skip)
                {
                    skip--;
                    wqty++;
                    continue;
                }
                int wr = _writePrimitives(inf.Type, src, dst, out int r, out int w);
                if (0 == wr)
                {
                    wqty++;
                    readed += r;
                    writed += w;
                    src = src.Slice(r);
                    dst = dst.Slice(w);
                    WriteSep(SepVar, ref dst, ref writed);
                }
                else
                    return wr;
            }
        }
        if (0 == skip && 1 < totalElement)
            WriteSep(SepEndArray, ref dst, ref writed);
        return 0;
    }

    public byte[]? SepBeginStruct = null;
    public byte[]? SepEndStruct = null;
    public byte[]? SepBeginArray = null;
    public byte[]? SepEndArray = null;
    public byte[]? SepVar = null;
    public byte[]? SepRecBegin = null;
    public byte[]? SepRecEnd = null;

    public int WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int writed)
    {
        if (null == src || 0 == src.Length)
            return 0;
        if (dst.Length < src.Length)
            return 1;
        src.CopyTo(dst);
        dst = dst.Slice(src.Length);
        writed += src.Length;
        return 0;
    }

    public delegate int WritePrimitivesFn(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int readed, out int writed);

    public StructWriter(WritePrimitivesFn writePrimitivesFn)
    {
        _writePrimitives = writePrimitivesFn;
    }

    WritePrimitivesFn _writePrimitives;

}
