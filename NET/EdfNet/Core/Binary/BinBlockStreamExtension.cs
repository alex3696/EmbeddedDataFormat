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
            stream.ReadExactly(block.Buffer[..1]);
        }
        while (!Enum.IsDefined(block.Type));

        stream.ReadExactly(block.Buffer.Slice(1, 2));
        var contentLen = block.ContentLen;
        if (0 < contentLen)
        {
            int dataLenAndCrcLen = contentLen + BinBlock.CrcLen;
            stream.ReadExactly(block.Buffer.Slice(BinBlock.HeaderLen, dataLenAndCrcLen));
            BinaryBlockIntegrityException.ThrowIfCrcWrong(block);
        }
        return BinBlock.OverheadLen + contentLen;
    }
}
