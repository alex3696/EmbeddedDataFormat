namespace EdfNet.Gen;

public class BinReader : BaseReaderBin
{
    public BinReader(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {

    }

    public EdfErr ReadData<TEnumerator>(ref TEnumerator enumerator)
           where TEnumerator : struct, IEdfByteEnumerator
    {
        while (enumerator.MoveNext())
        {
            int bytesRead = enumerator.Read(_blkData.CurrentData.Slice(_byteOffset));
            if (0 >= bytesRead)
            {
                bool isReaded = ReadBlock();
                if (!isReaded)
                    return EdfErr.SrcDataRequred;
                _byteOffset = 0;
                bytesRead = enumerator.Read(_blkData.CurrentData.Slice(_byteOffset));
            }
            _byteOffset += (ushort)bytesRead;
        }
        return EdfErr.IsOk;
    }

}
