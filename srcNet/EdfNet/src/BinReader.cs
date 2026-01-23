namespace NetEdf.src;

public class BinReader : IDisposable
{
    public readonly Header Cfg;
    readonly byte[] _data;
    readonly BinaryReader _br;
    private BinBlock? _current = default;
    public UInt16 EqQty;
    public byte Seq;

    readonly StructWriter _bw;

    public BinReader(Stream stream, Header? header, StructWriter.WritePrimitivesFn fn)
    {
        _br = new BinaryReader(stream);
        _data = new byte[16];
        if (header == null && TryGet(out BinBlock? bb)
            && BlockType.Header == bb.Type)
        {
            Cfg = Header.Parse(bb.Data);
            Clear();
        }
        else
            Cfg = Header.Default;
        _data = new byte[Cfg.Blocksize];

        _bw = new StructWriter(fn);
    }

    public void Dispose()
    {
        _br.Dispose();
    }

    public bool TryReadHeader([NotNullWhen(true)] out Header? h)
    {
        if (TryGet(out BinBlock? blk)
            && blk.Type == BlockType.Header
            && 16 == blk.Data.Length
            && 0xbd == blk.Data[0] && 0xf0 == blk.Data[1])
        {
            h = Header.Parse(blk.Data);
            Clear();
            return true;
        }
        h = default;
        return false;
    }
    public bool TryReadVarInfo([NotNullWhen(true)] out TypeInfo? v)
    {
        if (TryGet(out BinBlock? blk)
            && blk.Type == BlockType.VarInfo)
        {
            v = TypeInfo.Parse(blk.Data);
            Clear();
            return true;
        }
        v = default;
        return false;
    }
    public bool TryReadData(TypeInfo inf, ReadOnlySpan<byte> src, [NotNullWhen(true)] out List<byte[]>? ret)
    {
        List<byte[]> r = [];
        byte[] buff = new byte[255];
        int wr;
        do
        {
            wr = _bw.WriteSingleValue(inf, src, buff, out var rd, out var wd);
            src = src.Slice(rd);
            if (0 == wr)
            {
                r.Add(buff.AsSpan(0, wd).ToArray());
            }
        }
        while (wr == 0);
        ret = r;
        return true;
    }
    public bool TryReadVarData(TypeInfo inf, [NotNullWhen(true)] out List<byte[]>? v)
    {
        if (TryGet(out BinBlock? blk))
        {
            bool isOk = false;
            List<byte[]>? values = null;
            switch (blk.Type)
            {
                default: break;
                case BlockType.VarData: isOk = TryReadData(inf, blk.Data, out values); break;
            }
            if (isOk)
            {
                v = (values is null) ? [] : values;
                Clear();
                return true;
            }
        }
        v = default;
        return false;
    }
    public bool TryReadVar([NotNullWhen(true)] out Var? v)
    {
        if (TryReadVarInfo(out var inf))
        {
            v = new Var(inf);

            List<byte[]> values = [];
            while (TryReadVarData(inf, out var d))
            {
                values.AddRange(d);
            }
            v.Values = values;
            return true;
        }
        v = default;
        return false;
    }

    public BinBlock? Get() => _current ?? ReadBlock();
    public bool TryGet([NotNullWhen(true)] out BinBlock? blk)
    {
        blk = Get();
        return blk != null;
    }
    public BinBlock? Clear() => _current = null;

    private BinBlock? ReadBlock()
    {
        try
        {
            if (null != _current && 0 < EqQty)
            {
                EqQty--;
                return _current;
            }
            BlockType t = (BlockType)_br.ReadByte();
            if (Enum.IsDefined(typeof(BlockType), t))
            {
                var seq = _br.ReadByte();
                var s = _br.ReadUInt16();
                if (0 < s)
                {
                    _br.Read(_data, 0, s);
                    _current = new BinBlock(t, _data, s);
                    return _current;
                }
            }
        }
        catch (Exception ex)
        {
        }
        _current = null;
        return _current;
    }
}
