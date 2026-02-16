namespace NetEdf.src;

public class BinWriter : BaseWriter
{
    public ushort CurrentQty => _blkQty;
    private readonly Stream _bw;

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

    protected byte _blkSeq;
    private void WriteBlock(ReadOnlySpan<byte> data, BlockType blkType)
    {
        _bw.WriteByte((byte)blkType);
        _bw.WriteByte(_blkSeq);
        _bw.Write(BitConverter.GetBytes(_blkQty));
        _bw.Write(data);
        ushort crc = ModbusCRC.Calc([(byte)blkType]);
        crc = ModbusCRC.Calc([_blkSeq], crc);
        crc = ModbusCRC.Calc(BitConverter.GetBytes(_blkQty), crc);
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
        _blkData.AsSpan(0, 16).Clear();
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.UInt8, h.VersMajor);
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.UInt8, h.VersMinor);
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.UInt16, h.Encoding);
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.UInt16, h.Blocksize);
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.UInt32, h.Flags);
        _blkQty = 16;
        WriteBlock(_blkData.AsSpan(0, 16), BlockType.Header);
    }
    public override void Write(TypeRec t)
    {
        Flush();
        using var ms = new MemoryStream(_blkData);
        Primitives.SrcToBin(ms, PoType.UInt32, t.Id);
        BinWriter.Write(ms, t.Inf);
        _blkQty += (ushort)ms.Position;
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.String, t.Name ?? string.Empty);
        _blkQty += (ushort)Primitives.SrcToBin(_EmptySpan, PoType.String, t.Desc ?? string.Empty);
        _currDataType = t.Inf;
        WriteBlock(_blkData.AsSpan(0, _blkQty), BlockType.VarInfo);
    }

    private static long Write(Stream dst, TypeInf inf)
    {
        var begin = dst.Position;
        var bw = new BinaryWriter(dst);
        bw.Write((byte)inf.Type);
        if (null != inf.Dims && 0 < inf.Dims.Length)
        {
            bw.Write((byte)inf.Dims.Length);
            for (int i = 0; i < inf.Dims.Length; i++)
                bw.Write(inf.Dims[i]);
        }
        else
        {
            bw.Write((byte)0);
        }
        EdfBinString.WriteBin(inf.Name, dst);

        if (PoType.Struct == inf.Type && null != inf.Childs && 0 < inf.Childs.Length)
        {
            bw.Write((byte)inf.Childs.Length);
            for (int i = 0; i < inf.Childs.Length; i++)
            {
                Write(dst, inf.Childs[i]);
            }
        }
        return dst.Position - begin;
    }

}
