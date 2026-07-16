namespace EdfNet.Gen;

public class BinReader : BaseReaderBin
{
    public BinReader(Stream stream, Config? cfg = default)
        :base(stream, cfg)
    {

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
