namespace NetEdf.src;

public class BinReader : IDisposable
{
    public readonly Header Cfg;
    readonly byte[] _data;
    readonly BinaryReader _br;
    private BinBlock _current;
    public UInt16 EqQty;
    public byte Seq;

    public UInt16 Pos;

    readonly StructWriter _bw;

    public BinReader(Stream stream, Header? header = default, StructWriter.WritePrimitivesFn? fn = default)
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

        _bw = new StructWriter(fn ?? Primitives.BinToBin);
        _current = new BinBlock(0, new byte[Cfg.Blocksize], 0);
    }

    public bool ReadBlock()
    {
        BlockType t = (BlockType)_br.ReadByte();
        if (Enum.IsDefined(t))
        {
            var seq = _br.ReadByte();
            var len = _br.ReadUInt16();
            if (0 < len)
            {
                _current.Type = t;
                _current.Seq = seq;
                _current.Qty = len;
                _br.Read(_current._data, 0, len);
                Pos = 0;
                return true;
            }
        }
        return false;
    }
    public BlockType GetBlockType() => _current.Type;
    public byte GetBlockSeq() => _current.Seq;
    public ushort GetBlockLen() => _current.Qty;
    public Span<byte> GetBlockData() => _current._data.AsSpan(0, _current.Qty);

    public Header? ReadHeader()
    {
        if (_current.Type == BlockType.Header)
            return Header.Parse(_current.Data);
        return null;
    }
    public TypeRec? ReadInfo()
    {
        if (_current.Type == BlockType.VarInfo)
        {
            TypeRec rec = new()
            {
                Id = BinaryPrimitives.ReadInt32LittleEndian(_current._data),
                Inf = TypeInf.Parse(_current.Data.Slice(sizeof(uint))),
            };
            return rec;
        }
        return null;
    }

    public static int ReadBin(TypeInf t, Span<byte> src, Type csType, out object? ret)
    {
        if (!Enum.IsDefined(t.Type))
            throw new ArgumentOutOfRangeException($"wrong PoType={t.Type}");
        switch (t.Type)
        {
            default:
            case PoType.Struct:
                ret = default;
                if (null == t.Items)
                    return 0;
                ret = Activator.CreateInstance(csType);
                var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
                int fieldId = 0;
                int readed = 0;
                foreach (var child in t.Items)
                {
                    var field = fields[fieldId++];
                    readed += ReadBin(child, src, field.GetType(), out var childVal);
                    if (0 < readed)
                    {
                        field.SetValue(ret, childVal);
                    }
                    src = src.Slice(readed);
                }
                return readed;
            case PoType.Char:
            case PoType.UInt8: ret = MemoryMarshal.Read<byte>(src); return t.Type.GetSizeOf();
            case PoType.Int8: ret = MemoryMarshal.Read<sbyte>(src); return t.Type.GetSizeOf();
            case PoType.UInt16: ret = MemoryMarshal.Read<ushort>(src); return t.Type.GetSizeOf();
            case PoType.Int16: ret = MemoryMarshal.Read<short>(src); return t.Type.GetSizeOf();
            case PoType.UInt32: ret = MemoryMarshal.Read<uint>(src); return t.Type.GetSizeOf();
            case PoType.Int32: ret = MemoryMarshal.Read<int>(src); return t.Type.GetSizeOf();
            case PoType.UInt64: ret = MemoryMarshal.Read<ulong>(src); return t.Type.GetSizeOf();
            case PoType.Int64: ret = MemoryMarshal.Read<long>(src); return t.Type.GetSizeOf();
            case PoType.Half: ret = MemoryMarshal.Read<Half>(src); return t.Type.GetSizeOf();
            case PoType.Single: ret = MemoryMarshal.Read<float>(src); return t.Type.GetSizeOf();
            case PoType.Double: ret = MemoryMarshal.Read<double>(src); return t.Type.GetSizeOf();
            case PoType.String:
                var len = (byte)int.Min(0xFE, src[0]);
                if (0 < len)
                {
                    ret = Encoding.UTF8.GetString(src.Slice(1, len));
                    return (ushort)(1 + len);
                }
                else
                {
                    ret = string.Empty;
                    return 1;
                }
        }
    }



    public int TryRead<T>(TypeInf t, [NotNullWhen(true)] out T? ret)
    {
        int readed = ReadBin(t, _current._data, typeof(T), out var result);
        if (null != result)
            ret = (T)Convert.ChangeType(result, typeof(T));
        else
            ret = default;
        return readed;
    }

    public void Dispose()
    {
        _br.Dispose();
    }

    public bool TryReadHeader([NotNullWhen(true)] out Header? h)
    {
        if (TryGet(out BinBlock? blk)
            && blk.Type == BlockType.Header
            && 16 == blk.Data.Length)
        {
            h = Header.Parse(blk.Data);
            Clear();
            return true;
        }
        h = default;
        return false;
    }
    public bool TryReadVarInfo([NotNullWhen(true)] out TypeInf? v)
    {
        if (TryGet(out BinBlock? blk)
            && blk.Type == BlockType.VarInfo)
        {
            v = TypeInf.Parse(blk.Data);
            Clear();
            return true;
        }
        v = default;
        return false;
    }
    public bool TryReadData(TypeInf inf, ReadOnlySpan<byte> src, [NotNullWhen(true)] out List<byte[]>? ret)
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
    public bool TryReadVarData(TypeInf inf, [NotNullWhen(true)] out List<byte[]>? v)
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

    public BinBlock? Get() => _current;
    public bool TryGet([NotNullWhen(true)] out BinBlock? blk)
    {
        blk = Get();
        return blk != null;
    }
    public BinBlock? Clear() => _current = null;

    private BinBlock? ReadBlock1()
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
