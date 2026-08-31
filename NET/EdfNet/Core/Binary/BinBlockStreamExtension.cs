namespace EdfNet.Core.Binary;

public static class BinBlockStreamExtension
{
    public static int Write(this Stream stream, BinBlock block)
    {
        if (!Enum.IsDefined(block.Type))
            throw new ArgumentException(nameof(block.Type));
        ArgumentOutOfRangeException.ThrowIfEqual(block.ContentLen, 0);
        block.UpdateCrc();
        var bb = block.BinaryBlock;
        stream.Write(bb);
        return bb.Length;
    }

    public static int Read(this Stream stream, BinBlock block)
    {
        do
        {
            if (1 != stream.Read(block.Buffer[..1]))
                throw new EndOfStreamException();
        }
        while (!Enum.IsDefined(block.Type));
        var spanContentLen = block.Buffer.Slice(1, 2);

        if (2 != stream.Read(spanContentLen))
            throw new EndOfStreamException();
        if (0 < block.ContentLen)
        {
            int dataLenAndCrcLen = block.ContentLen + BinBlock.CrcLen;
            int readed = stream.Read(block.ContentBuffer[..dataLenAndCrcLen]);
            if (readed != dataLenAndCrcLen)
                throw new EndOfStreamException();
            if (!block.CheckCrc())
                throw new Exception($"Wrong CRC block");
        }
        return BinBlock.OverheadLen + block.ContentLen;
    }
}
