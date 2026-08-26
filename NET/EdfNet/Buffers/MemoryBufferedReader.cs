namespace EdfNet.Buffers;

/// <summary>
/// Адаптер IBufferedReader для уже загруженных в память данных.
/// </summary>
public class MemoryBufferedReader : IBufferedReader
{
    private readonly ReadOnlyMemory<byte> _memory;
    private int _position;
    private long _consumed;

    public MemoryBufferedReader(byte[] data) : this(data.AsMemory()) { }

    public MemoryBufferedReader(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    public ReadOnlySpan<byte> GetSpan(int minimumLength = 0)
    {
        return _memory.Span.Slice(_position);
    }

    public void Advance(int count)
    {
        if (count < 0 || _position + count > _memory.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        _position += count;
        _consumed += count;
    }

    public long Consumed => _consumed;
}
