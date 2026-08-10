namespace EdfNet.Gen;

public class WriterTxt : BaseWriterBin
{
    public WriterTxt(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {
        _rstate = new(_stream, _blkData);
    }

    public override void Write(Schema sch)
    {
        base.Write(sch);
        _rstate.Skip = 0;
        _rstate.RecordId = 0;
        _rstate.PrimOffset = 0;
    }
    private readonly RecursiveWriterState _rstate;

    public override EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enm)
        where TEnumerator : struct
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        var writer = new RecursiveWriterBin<TEnumerator>(_rstate, CurrentSchema.Type, ref enm, true);
        return writer.DoWrite();
    }
}
