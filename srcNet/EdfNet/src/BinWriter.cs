namespace NetEdf.src;

public class BinWriter : BaseWriter
{
    public ushort CurrentQty => _blkQty;
    private readonly Stream _bw;
    protected byte _blkSeq;

    protected override EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w)
        => Primitives.TrySrcToBin(t, obj, dst, out w);
    protected override EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
        => EdfErr.IsOk;

    public BinWriter(Stream stream, Header? cfg = default)
        : base(cfg ?? Header.Default)
    {
        _bw = stream;
        Write(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _bw.Flush();
        base.Dispose(disposing);
    }
    private void WriteBlock(ReadOnlySpan<byte> data, BlockType blkType)
    {
        var blkQty = (ushort)data.Length;
        _bw.WriteByte((byte)blkType);
        _bw.WriteByte(_blkSeq);
        _bw.Write(BitConverter.GetBytes(blkQty));
        _bw.Write(data);
        ushort crc = ModbusCRC.Calc([(byte)blkType]);
        crc = ModbusCRC.Calc([_blkSeq], crc);
        crc = ModbusCRC.Calc(BitConverter.GetBytes(blkQty), crc);
        crc = ModbusCRC.Calc(data, crc);
        _bw.Write(BitConverter.GetBytes(crc));
        _blkSeq++;
        _blkQty = 0;
    }
    public override void Flush()
    {
        if (null == _currDataType || 0 == _blkQty)
            return;
        WriteBlock(_blkData.AsSpan(0, _blkQty), BlockType.VarData);
    }
    public override void Write(Header h)
    {
        Flush();
        _currDataType = null;
        var dst = _blkData.AsSpan(0, 16);
        dst.Clear();
        dst.SrcToBinRef(PoType.UInt8, h.VersMajor);
        dst.SrcToBinRef(PoType.UInt8, h.VersMinor);
        dst.SrcToBinRef(PoType.UInt16, h.Encoding);
        dst.SrcToBinRef(PoType.UInt16, h.Blocksize);
        dst.SrcToBinRef(PoType.UInt32, h.Flags);
        WriteBlock(_blkData.AsSpan(0, 16), BlockType.VarData);
    }
    public override void Write(TypeRec t)
    {
        Flush();
        var dst = _blkData.AsSpan();
        dst.SrcToBinRef(PoType.UInt32, t.Id);
        Write(ref dst, t.Inf);
        dst.SrcToBinRef(PoType.String, t.Name ?? string.Empty);
        dst.SrcToBinRef(PoType.String, t.Desc ?? string.Empty);
        _currDataType = t.Inf;
        WriteBlock(_blkData.AsSpan(0, _blkData.Length - dst.Length), BlockType.VarInfo);
    }
    private static long Write(ref Span<byte> dst, TypeInf inf)
    {
        var begin = dst.Length;
        dst.SrcToBinRef(PoType.UInt8, inf.Type);
        if (null != inf.Dims && 0 < inf.Dims.Length)
        {
            dst.SrcToBinRef(PoType.UInt8, (byte)inf.Dims.Length);
            for (int i = 0; i < inf.Dims.Length; i++)
                dst.SrcToBinRef(PoType.UInt32, inf.Dims[i]);
        }
        else
        {
            dst.SrcToBinRef(PoType.UInt8, (byte)0);
        }
        dst.SrcToBinRef(PoType.String, inf.Name ?? string.Empty);

        if (PoType.Struct == inf.Type && null != inf.Childs && 0 < inf.Childs.Length)
        {
            dst.SrcToBinRef(PoType.UInt8, (byte)inf.Childs.Length);
            for (int i = 0; i < inf.Childs.Length; i++)
            {
                Write(ref dst, inf.Childs[i]);
            }
        }
        return begin - dst.Length;
    }
}
