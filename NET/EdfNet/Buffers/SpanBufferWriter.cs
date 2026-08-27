using EdfNet.Core.Binary;
using System.Buffers;

namespace EdfNet.Buffers;

public ref struct SpanBufferWriter : IBufferWriter<byte>
{
    private readonly Span<byte> _buf;
    int _index;

    public SpanBufferWriter(Span<byte> sp)
    {
        _buf = sp;
        _index = 0;
    }

    public void Advance(int count)
    {
        if (count < 0 || _index + count > _buf.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        throw new NotImplementedException();
    }

    public readonly Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint > 0)
        {
            //if (_index + sizeHint > _mem.Length)
            //    throw new ArgumentOutOfRangeException(nameof(sizeHint));
            return _buf.Slice(_index, sizeHint);
        }
        return _buf.Slice(_index);
    }
    public void Clear() => _index = 0;
    public readonly int WrittedCount => _index;


    public int Append<T>(T val) where T : struct
    {
        var valLen = Unsafe.SizeOf<T>();
        MemoryMarshal.Write(GetSpan(valLen), val);
        Advance(valLen);
        return valLen;
    }
    public int Append(string? str)
    {
        int writed = EdfBinString.WriteBin(str, GetSpan());
        ArgumentOutOfRangeException.ThrowIfLessThan(writed, 1);
        Advance(writed);
        return writed;
    }
}
