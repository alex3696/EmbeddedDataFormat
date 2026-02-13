namespace NetEdf.src;

public class BinReader : BaseReader
{
    public readonly Header Cfg;
    readonly BinaryReader _br;
    private readonly BinBlock _current;
    public UInt16 EqQty;
    public byte Seq;

    public UInt16 Pos;
    protected TypeInf? _currDataType;

    public BinReader(Stream stream, Header? header = default)
    {
        _br = new BinaryReader(stream);
        //Span<byte> b = stackalloc byte[t.GetSizeOf()]; 
        _current = new BinBlock(0, new byte[256], 0);

        Cfg = Header.Default;
        if (ReadBlock())
            Cfg = ReadHeader() ?? Header.Default;

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

                if (Cfg.Flags.HasFlag(Options.UseCrc))
                {
                    ushort fileCrc = _br.ReadUInt16();
                    ushort crc = ModbusCRC.Calc([(byte)_current.Type]);
                    crc = ModbusCRC.Calc([_current.Seq], crc);
                    crc = ModbusCRC.Calc(BitConverter.GetBytes(_current.Qty), crc);
                    crc = ModbusCRC.Calc(_current.Data, crc);
                    if (crc != fileCrc)
                        throw new Exception($"Wrong CRC block {_current.Seq}");
                }
                if (_current.Type != BlockType.VarData)
                    _currDataType = ReadInfo()?.Inf;
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
                Inf = Parse(_current.Data.Slice(sizeof(uint))),
            };
            return rec;
        }
        return null;
    }

    public static EdfErr ReadBin(TypeInf t, ReadOnlySpan<byte> src, Type csType, out int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
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
                if (EdfErr.IsOk != (err = ReadObjElement(t, src, elementType, out var r, out var arr1)))
                    return err;
                arr.SetValue(arr1, i);
                readed += r;
                src = src.Slice(r);
            }
        }
        else
        {
            if (EdfErr.IsOk != (err = ReadObjElement(t, src, csType, out readed, out ret)))
                return err;
        }
        return err;
    }
    static EdfErr ReadObjElement(TypeInf t, ReadOnlySpan<byte> src, Type csType, out int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
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
                err = ReadBin(child, src, field.PropertyType, out var r, out var childVal);
                if (EdfErr.IsOk == err && 0 < r && null != childVal)
                {
                    field.SetValue(ret, childVal);
                }
                readed += r;
                src = src.Slice(r);
            }
        }
        else
        {
            if (0 != (err = Primitives.BinToSrc(t.Type, src, out var r, out ret)))
                return err;
            readed += r;
        }
        return err;
    }

    int _skip = 0;
    int _readed = 0;
    public EdfErr TryRead<T>([NotNullWhen(true)] out T? ret)
    {
        ArgumentNullException.ThrowIfNull(_currDataType);
        EdfErr err;
        ret = default;
        Span<byte> src = _current._data.AsSpan(_readed, _current.Qty - _readed);
        do
        {
            int skip = _skip;
            err = ReadBin(_currDataType, src, typeof(T), out var readed, out var result);
            src = src.Slice(readed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.DstBufOverflow: return err;
                case EdfErr.SrcDataRequred:
                    if (!ReadBlock())
                        return EdfErr.SrcDataRequred;
                    _readed = 0;
                    src = _current._data;
                    break;
                case EdfErr.IsOk:
                    ret = (T?)Convert.ChangeType(result, typeof(T));
                    _readed += readed;
                    return err;
            }
        }
        while (true);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        _br.Dispose();
    }


    public static TypeInf Parse(ReadOnlySpan<byte> b) => FromBytes(b, out _);
    static TypeInf FromBytes(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> rest)
    {
        rest = b;
        if (2 > rest.Length)
            throw new ArgumentException($"array is too small {b.Length}");
        if (!Enum.IsDefined(typeof(PoType), b[0]))
            throw new ArgumentException("type mismatch");
        // type
        var type = (PoType)b[0];
        rest = rest.Slice(1);
        // dim
        var dimsCount = rest[0];
        rest = rest.Slice(1);
        uint[]? dims = null;
        if (0 < dimsCount)
        {
            dims = new uint[dimsCount];
            for (int i = 0; i < dimsCount; i++)
            {
                dims[i] = BinaryPrimitives.ReadUInt32LittleEndian(rest);
                rest = rest.Slice(sizeof(UInt32));
            }
        }
        // name
        byte bNameSize = rest[0];
        rest = rest.Slice(1);
        if (255 < bNameSize)
            throw new ArgumentException("name len mismatch");
        var name = Encoding.UTF8.GetString(rest.Slice(0, bNameSize));
        rest = rest.Slice(bNameSize);
        // childs
        List<TypeInf>? childs = null;
        if (PoType.Struct == type && 0 < rest.Length)
        {
            byte childsCount = rest[0];
            rest = rest.Slice(1);
            childs = new List<TypeInf>(childsCount);
            for (int i = 0; i < childsCount; i++)
                childs.Add(FromBytes(rest, out rest));
        }
        return new TypeInf(name, type, dims, childs?.ToArray());
    }
}
