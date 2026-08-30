namespace EdfNet.Buffers;

/// <summary>
/// Буферизированный reader для Stream.
/// Сдвигает остаток в начало и дочитывает поток только если остаток мньше MinThreshold = 256 байт
/// или недостаточен для запрошенного minimumLength.
/// </summary>
public sealed class StreamBufferedReader : IBufferedReader
{
    public ushort MinThreshold { get; set; } = 256;
    private readonly Stream _stream;
    private readonly byte[] _buffer;
    private int _start;
    private int _end;
    private long _consumed;

    public StreamBufferedReader(Stream stream, byte[] buffer)
    {
        _stream = stream;
        _buffer = buffer;
    }
    public StreamBufferedReader(Stream stream, int bufferSize = 1024)
        : this(stream, new byte[bufferSize])
    {
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetSpan(int minimumLength = 0)
    {
        int available = _end - _start;
        // Упредительное чтение: подгружаем только если остаток мал или не покрывает запрос
        if (available < minimumLength || available < MinThreshold)
            DownloadBlock();
        return new ReadOnlySpan<byte>(_buffer, _start, _end - _start);
    }
    private void DownloadBlock()
    {
        int available = _end - _start;
        if (_start > 0)
        {
            if (available > 0)
                _buffer.AsSpan(_start, available).CopyTo(_buffer);
            _end = available;
            _start = 0;
        }
        while (_end < _buffer.Length)
        {
            int read = _stream.Read(_buffer, _end, _buffer.Length - _end);
            if (read == 0) break;
            _end += read;
        }
    }

    public void Advance(int count)
    {
        if (count < 0 || _start + count > _end)
            throw new ArgumentOutOfRangeException(nameof(count));
        _start += count;
        _consumed += count;
    }

    public long Consumed => _consumed;
}
