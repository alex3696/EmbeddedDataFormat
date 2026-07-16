namespace EdfNet.Ref;

public class WriterBin : BaseWriterBin
{
    private readonly RecursiveWriterBin _writer;

    public WriterBin(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _writer = new(_blkData, _stream);
    }
    public override EdfErr Write(object obj)
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        return _writer.DoWrite(CurrentSchema.Type, obj);
    }

    public override EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct
    {
        throw new NotImplementedException();
    }
}
