using System.Collections.Concurrent;

namespace EdfNet.Gen;

public class BinWriter : BaseDisposable, IWriter
{
    private readonly Config _cfg;
    private Schema? _currentSchema;
    private readonly Stream _bw;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _blk;
    private readonly BinDataBlock _blkData;
    private uint _recId = 0;
    private ushort _prmOffset = 0;

    public BinWriter(Stream stream, Config? cfg = default)
        : base()
    {
        _cfg = cfg ?? Config.Default;
        _bw = stream;
        _blkBuf = new byte[_cfg.Blocksize];
        _blk = new(_blkBuf);
        _blkData = new(_blkBuf);
        if (0 == stream.Position)
            Write(_cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _bw.Flush();
        base.Dispose(disposing);
    }
    public void Flush()
    {
        switch (_blk.Type)
        {
            default: break;
            case BlockType.Config:
            case BlockType.Schema:
                if (0 < _blk.ContentLen)
                {
                    _bw.Write(_blk);
                    _blk.Reset();
                }
                break;
            case BlockType.Data:
                if (null != _currentSchema && 0 != _blkData.DataLen)
                {
                    _bw.Write(_blk);
                    PrepareNewBlock();
                }
                break;
        }
    }
    public void Write(Config h)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Config;
        _blk.Append(h.VersMajor);
        _blk.Append(h.VersMinor);
        _blk.Append(h.Encoding);
        _blk.Append(h.Blocksize);
        _blk.Append((ushort)0);
        _blk.Append(h.Flags);
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blk.ContentLen, 12);
        _bw.Write(_blk);
        _blk.Reset();
    }
    public void Write(Schema sch)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Schema;
        _blk.Append(sch.Id);
        _blk.Append(sch.Name);
        _blk.Append(sch.Desc);
        Append(_blk, sch.Type);
        _bw.Write(_blk);
        _blk.Reset();
        _currentSchema = sch;
        _blk.Type = BlockType.Data;
        _recId = 0;
        _prmOffset = 0;
        PrepareNewBlock();
    }

    private static void Append(BinBlock blk, EdfType t)
    {
        blk.Append(t.Type);
        if (null != t.Dims && 0 < t.Dims.Length)
        {
            blk.Append((byte)t.Dims.Length);
            for (int i = 0; i < t.Dims.Length; i++)
                blk.Append(t.Dims[i]);
        }
        else
            blk.Append((byte)0);

        blk.Append(t.Name);

        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            blk.Append((byte)t.Childs.Length);
            for (int i = 0; i < t.Childs.Length; i++)
            {
                Append(blk, t.Childs[i]);
            }
        }
    }

    // Потокобезопасный кэш для хранения скомпилированных методов записи под каждый тип
    private static readonly ConcurrentDictionary<Type, Action<BinWriter, object>> _writerCache = new();

    public EdfErr Write(object? obj)
    {
        throw new NotImplementedException();
    }

    public EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        // Берем срез свободного места в текущем буфере блока
        Span<byte> _blockDataBuffer = _blkData.DataBuffer.Slice(_blkData.DataLen);
        while (enumerator.MoveNext())
        {
            // Пробуем записать примитив в доступный остаток блока
            int bytesWritten = enumerator.Write(_blockDataBuffer);
            if (0 >= bytesWritten)// Если вернулся меньше 0, значит примитив целиком не поместился (попримитивный разрыв).
            {
                Flush();// Сбрасываем (Flush) текущий блок на диск/в поток и очищаем буфер
                // Подготавливаем новый блок, записывая в заголовок SchId, RecId и тип текущего примитива
                PrepareNewBlock();
                // Пересчитываем срез свободного места для абсолютно нового, чистого блока
                _blockDataBuffer = _blkData.DataBuffer;
                // Пробуем записать примитив еще раз, теперь уже в начало нового блока
                bytesWritten = enumerator.Write(_blockDataBuffer);
                if (0 >= bytesWritten) // Защита от бесконечного цикла (если примитив физически больше размера блока)
                    return EdfErr.DstBufOverflow;
            }
            _prmOffset++;
            // Фиксируем, сколько байт реально записал энумератор в буфер блока
            _blkData.DataLen += (ushort)bytesWritten;

            // Сдвигаем наш Span вперед, отрезая уже заполненную часть памяти
            _blockDataBuffer = _blockDataBuffer.Slice(bytesWritten);
        }
        _recId++;
        return EdfErr.IsOk;
    }

    void PrepareNewBlock()
    {
        ArgumentNullException.ThrowIfNull(_currentSchema);
        _blkData.Clear();
        _blkData.SchemaId = _currentSchema.Id;
        _blkData.RecordId = _recId;
        _blkData.PrimOffset = _prmOffset;
    }
}
