namespace NetEdf.src;

public class BinReader : IDisposable
{
    public readonly Header Cfg;
    readonly BinaryReader _br;
    private readonly BinBlock _current;
    public UInt16 EqQty;
    public byte Seq;

    public UInt16 Pos;

    public BinReader(Stream stream, Header? header = default)
    {
        _br = new BinaryReader(stream);
        //Span<byte> b = stackalloc byte[t.GetSizeOf()]; 
        _current = new BinBlock(0, new byte[256], 0);

        if (ReadBlock())
            Cfg = ReadHeader() ?? Header.Default;
        else
            Cfg = Header.Default;
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
                Id = BinaryPrimitives.ReadUInt32LittleEndian(_current._data),
                Inf = TypeInf.Parse(_current.Data.Slice(sizeof(uint))),
            };
            return rec;
        }
        return null;
    }

    public static int ReadBin(TypeInf t, ReadOnlySpan<byte> src, Type csType, out int readed, out object? ret)
    {
        if (!Enum.IsDefined(t.Type))
            throw new ArgumentOutOfRangeException($"wrong PoType={t.Type}");
        readed = 0;
        ret = default;

        uint totalElement = 1;
        for (int i = 0; i < t.Dims?.Length; i++)
            totalElement *= t.Dims[i];

        if (1 < totalElement)
        {
            if (!csType.IsArray)
                throw new ArrayTypeMismatchException();
            var elementType = csType.GetElementType();
            ArgumentNullException.ThrowIfNull(elementType);
            var arr = Array.CreateInstance(elementType, totalElement);
            ret = arr;
            for (int i = 0; i < totalElement; i++)
            {
                var rv = ReadObjElement(t, src, elementType, out var r, out var arr1);
                if (0 != rv)
                    return rv;
                arr.SetValue(arr1, i);
                readed += r;
                src = src.Slice(r);
            }
        }
        else
        {
            var rv = ReadObjElement(t, src, csType, out readed, out ret);
            if (0 != rv)
                return rv;
        }
        return 0;

    }
    static int ReadObjElement(TypeInf t, ReadOnlySpan<byte> src, Type csType, out int readed, out object? ret)
    {
        readed = 0;
        if (PoType.Struct == t.Type)
        {
            ret = default;
            if (null == t.Items)
                return 0;
            ret = Activator.CreateInstance(csType);
            var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
            int fieldId = 0;
            foreach (var child in t.Items)
            {
                var field = fields[fieldId++];
                var rv = ReadBin(child, src, field.PropertyType, out var r, out var childVal);
                if (0 < r && null != childVal)
                {
                    field.SetValue(ret, childVal);
                }
                readed += r;
                src = src.Slice(r);
            }
        }
        else
        {
            int rv = Primitives.BinToSrc(t.Type, src, ref readed, out ret);
            if (0 != rv)
                return rv;
        }
        return 0;
    }



    public int TryRead<T>(TypeInf t, [NotNullWhen(true)] out T? ret)
    {
        int readed = ReadBin(t, _current._data, typeof(T), out var r, out var result);
        if (null != result)
            ret = (T)Convert.ChangeType(result, typeof(T));
        else
            ret = default;
        return r;
    }

    public void Dispose()
    {
        _br.Dispose();
    }

}
