namespace EdfNet.Gen;

public class BinReader : BaseReader
{
    public readonly Config Cfg;
    private readonly BinaryReader _br;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _current;
    private readonly BinDataBlock _blkData;
    protected Schema? CurrentSchema;

    public BinReader(Stream stream, Config? cfg = default)
    {
        _br = new BinaryReader(stream);
        Cfg = cfg ?? Config.Default;
        _current = new(Cfg.Blocksize);
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
            if (_current.Type == BlockType.Schema)
            {
                CurrentSchema = ReadSchema();
            }
            return true;
        }
        return false;
    }
    public BlockType GetBlockType() => _current.Type;
    public ushort GetBlockLen() => _current.TotalLen;
    public ReadOnlySpan<byte> GetBlockData() => _current.CurrentContent;

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
        return new Schema()
        {
            Id = id,
            Name = name,
            Desc = desc,
            Type = type
        };
    }

    public EdfErr ReadData<TEnumerator>(ref TEnumerator enumerator)
           where TEnumerator : struct, IEdfByteEnumerator
    {
        ReadOnlySpan<byte> _blockDataBuffer = [];
        while (enumerator.MoveNext())
        {
            int bytesRead = enumerator.Read(_blockDataBuffer);
            if (0 >= bytesRead)
            {
                bool isReaded = ReadBlock();
                if (!isReaded)
                    return EdfErr.SrcDataRequred;
                _blockDataBuffer = _blkData.DataBuffer;
                bytesRead = enumerator.Read(_blockDataBuffer);
            }
            _blockDataBuffer = _blockDataBuffer.Slice(bytesRead);
        }
        return EdfErr.IsOk;
    }

}
