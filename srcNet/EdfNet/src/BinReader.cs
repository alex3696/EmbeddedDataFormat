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


    public static EdfErr ReadObject(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, totalElement, ref skip, ref qty, ref readed, ref ret);
        return ReadElement(t, src, ref skip, ref qty, ref readed, ref ret);
    }
    public static EdfErr ReadElement(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, ref skip, ref qty, ref readed, ref ret);
        return ReadPrimitive(t, src, ref skip, ref qty, ref readed, ref ret);
    }
    static EdfErr ReadArray(TypeInf t, ReadOnlySpan<byte> src, uint totalElement, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        Type csType = ret.GetType();
        if (!csType.IsArray)
            throw new ArrayTypeMismatchException();
        var elementType = csType.GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        var arr = ret as Array;
        ArgumentNullException.ThrowIfNull(arr);
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadElement(t, src, elementType, ref skip, ref qty, ref readed, out var arrItem)))
                return err;
            if (0 < readed)
            {
                arr.SetValue(arrItem, i);
                src = src.Slice(readed - r);
            }
        }
        return err;
    }
    static EdfErr ReadStruct(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == t.Items || 0 == t.Items.Length)
            return EdfErr.IsOk;
        Type csType = ret.GetType();
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
        int fieldId = 0;
        foreach (var child in t.Items)
        {
            var r = readed;
            var field = fields[fieldId++];
            if (EdfErr.IsOk != (err = ReadObject(child, src, field.PropertyType, ref skip, ref qty, ref readed, out var childVal)))
                return err;
            field.SetValue(ret, childVal);
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadPrimitive(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (0 < skip)
        {
            skip--;
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t.Type, src, out var r, out ret)))
            return err;
        readed += r;
        qty++;
        return err;
    }

    public static EdfErr ReadObject(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, csType, totalElement, ref skip, ref qty, ref readed, out ret);
        return ReadElement(t, src, csType, ref skip, ref qty, ref readed, out ret);
    }
    public static EdfErr ReadElement(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, csType, ref skip, ref qty, ref readed, out ret);
        return ReadPrimitive(t, src, csType, ref skip, ref qty, ref readed, out ret);
    }
    static EdfErr ReadArray(TypeInf t, ReadOnlySpan<byte> src, Type csType, uint totalElement, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (!csType.IsArray)
            throw new ArrayTypeMismatchException();
        var elementType = csType.GetElementType();
        ArgumentNullException.ThrowIfNull(elementType);
        var arr = Array.CreateInstance(elementType, totalElement);
        ret = arr;
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadElement(t, src, elementType, ref skip, ref qty, ref readed, out var arrItem)))
                return err;
            arr.SetValue(arrItem, i);
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadStruct(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        ret = default;
        if (null == t.Items || 0 == t.Items.Length)
            return EdfErr.IsOk;
        ret = Activator.CreateInstance(csType);
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
        int fieldId = 0;
        foreach (var child in t.Items)
        {
            var r = readed;
            var field = fields[fieldId++];
            if (EdfErr.IsOk != (err = ReadObject(child, src, field.PropertyType, ref skip, ref qty, ref readed, out var childVal)))
                return err;
            field.SetValue(ret, childVal);
            src = src.Slice(readed - r);
        }
        return err;
    }
    static EdfErr ReadPrimitive(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (0 < skip)
        {
            skip--;
            ret = null;
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t.Type, src, out var r, out ret)))
            return err;
        readed += r;
        qty++;
        return err;
    }


    int _skip = 0;
    int _readed = 0;
    object? _ret;
    public EdfErr TryRead<T>([NotNullWhen(true)] out T? ret)
    {
        ArgumentNullException.ThrowIfNull(_currDataType);
        EdfErr err;
        ret = default;
        Span<byte> src = _current._data.AsSpan(_readed, _current.Qty - _readed);
        do
        {
            int qty = 0;
            int skip = _skip;
            int readed = 0;

            if (null != _ret)
                err = ReadObject(_currDataType, src, ref skip, ref qty, ref readed, ref _ret);
            else
                err = ReadObject(_currDataType, src, typeof(T), ref skip, ref qty, ref readed, out _ret);
            src = src.Slice(readed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.DstBufOverflow: return err;
                case EdfErr.SrcDataRequred:
                    _skip += qty;
                    _readed = 0;
                    break;
                case EdfErr.IsOk:
                    ret = (T?)Convert.ChangeType(_ret, typeof(T));
                    _readed += readed;
                    _skip = 0;
                    _ret = null;
                    return err;
            }
        }
        while (err != EdfErr.SrcDataRequred);
        return err;
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
