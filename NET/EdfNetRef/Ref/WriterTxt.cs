namespace EdfNet.Ref;

public class WriterTxt : BaseWriterTxt
{
    private readonly RecursiveWriterTxt _writer;

    public WriterTxt(Stream stream, Config? cfg = null)
        : base(stream, cfg)
    {
        _writer = new(_st);
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
