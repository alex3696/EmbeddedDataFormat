namespace EdfNet.Core;

public class BinReader : BaseReader
{
    public readonly Config Cfg;
    private readonly BinaryReader _br;

    private readonly byte[] _blkBuf;
    private readonly BinBlock _current;
    private readonly BinDataBlock _blkData;


    protected Schema? CurrentSchema;
    private uint _recId = 0;
    private ushort _prmOffset = 0;  

    public BinReader(Stream stream, Config? cfg = default)
    {
        _br = new BinaryReader(stream);
        Cfg = cfg ?? Config.Default;
        _current = new BinBlock(new byte[32]);
        if (ReadBlock())
        {
            var newCfg = ReadHeader();
            if (newCfg != null)
                Cfg = newCfg;
        }
        _blkBuf = new byte[Cfg.Blocksize];
        _current = new(_blkBuf);
        _blkData = new(_blkBuf);
    }

    private void ReadDatBlockHeader()
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema, nameof(CurrentSchema));
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blkData.SchemaId, CurrentSchema.Id , nameof(BinDataBlock.SchemaId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blkData.RecordId, _recId, nameof(BinDataBlock.RecordId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blkData.PrimOffset, _prmOffset, nameof(BinDataBlock.PrimOffset));
    }

    public bool ReadBlock()
    {
        BlockType t;
        do
        {
            t = (BlockType)_br.ReadByte();
        }
        while (!Enum.IsDefined(t));

        var len = _br.ReadUInt16();

        if (0 < len)
        {
            _current.Type = t;
            _current.ContentLen = len;
            int dataLenAndCrcLen = len + BinBlock.CrcLen;
            int readed = _br.Read(_current.ContentBuffer[..dataLenAndCrcLen]);
            if (readed != dataLenAndCrcLen)
                return false;
            if (!_current.CheckCrc())
                throw new Exception($"Wrong CRC block");

            switch(_current.Type)
            {
                default: throw new Exception($"Wrong block Type: {_current.Type}");
                case BlockType.Config: break;
                case BlockType.Schema: CurrentSchema = ReadSchema(); break;
                case BlockType.Data: ReadDatBlockHeader(); break;
            }
            return true;
        }
        return false;
    }
    public BlockType GetBlockType() => _current.Type;
    public ushort GetBlockLen() => _current.TotalLen;
    public ReadOnlySpan<byte> GetBlockData() => _blkData.CurrentData;

    public Config? ReadHeader()
    {
        if (_current.Type != BlockType.Config)
            return null;
        var b = _current.CurrentContent;
        return new Config()
        {
            VersMajor = b[0],
            VersMinor = b[1],
            Encoding = MemoryMarshal.Read<ushort>(b[2..]),
            Blocksize = MemoryMarshal.Read<ushort>(b[4..]),
            Flags = MemoryMarshal.Read<Options>(b[8..]),
        };
    }
    public Schema? ReadSchema()
    {
        if (_current.Type != BlockType.Schema)
            return null;
        var b = _current.CurrentContent;
        int pos = 0;
        ushort id = MemoryMarshal.Read<ushort>(b[..sizeof(ushort)]);
        pos += sizeof(ushort);
        pos += EdfBinString.ReadBin(b[pos..], out string? name);
        pos += EdfBinString.ReadBin(b[pos..], out string? desc);
        var type = EdfType.Parse(b[pos..]);
        _recId = 0;
        _prmOffset = 0;
        return new Schema()
        {
            Id = id,
            Name = name,
            Desc = desc,
            Type = type
        };
    }


    public static EdfErr ReadObject(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, totalElement, ref skip, ref qty, ref readed, ref ret);
        return ReadElement(t, src, ref skip, ref qty, ref readed, ref ret);
    }
    public static EdfErr ReadElement(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, ref skip, ref qty, ref readed, ref ret);
        return ReadPrimitive(t, src, ref skip, ref qty, ref readed, ref ret);
    }
    static EdfErr ReadArray(EdfType t, ReadOnlySpan<byte> src, uint totalElement, ref int skip, ref int qty, ref int readed, ref object ret)
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
    static EdfErr ReadStruct(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        Type csType = ret.GetType();
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
        int fieldId = 0;
        foreach (var child in t.Childs)
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
    static EdfErr ReadPrimitive(EdfType t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
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

    public static EdfErr ReadObject(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, csType, totalElement, ref skip, ref qty, ref readed, out ret);
        return ReadElement(t, src, csType, ref skip, ref qty, ref readed, out ret);
    }
    public static EdfErr ReadElement(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, csType, ref skip, ref qty, ref readed, out ret);
        return ReadPrimitive(t, src, csType, ref skip, ref qty, ref readed, out ret);
    }
    static EdfErr ReadArray(EdfType t, ReadOnlySpan<byte> src, Type csType, uint totalElement, ref int skip, ref int qty, ref int readed, out object? ret)
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
    static EdfErr ReadStruct(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        ret = default;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        ret = Activator.CreateInstance(csType);
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
        int fieldId = 0;
        foreach (var child in t.Childs)
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
    static EdfErr ReadPrimitive(EdfType t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
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
    public EdfErr TryRead<T>(out T? ret)
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        EdfErr err;
        ret = default;
        ReadOnlySpan<byte> src = _blkData.CurrentData.Slice(_readed, _blkData.DataLen - _readed);
        do
        {
            int qty = 0;
            int skip = _skip;
            int readed = 0;

            if (null != _ret)
                err = ReadObject(CurrentSchema.Type, src, ref skip, ref qty, ref readed, ref _ret);
            else
                err = ReadObject(CurrentSchema.Type, src, typeof(T), ref skip, ref qty, ref readed, out _ret);
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
    }

}
