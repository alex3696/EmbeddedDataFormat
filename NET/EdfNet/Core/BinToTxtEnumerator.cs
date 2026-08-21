namespace EdfNet.Core;

class BlockReaderBin : BaseReaderBin
{
    public readonly BufStateBin State;
    public BlockReaderBin(Stream stream, Config? cfg = null)
        : base(stream, cfg)
    {
        State = new BufStateBin(stream, _blkData);
    }
    protected override void OnSchemaBlockRead()
    {
        if (CurrentSchema?.Type != null)
            State.Enum.Reset(CurrentSchema.Type);
    }
    protected override void OnReadDatBlockStart()
    {
        base.OnReadDatBlockStart();
        State.Readed = 0;
    }
}

class ConvWriterTxt : BaseWriterTxt
{
    public readonly BufStateTxt State;
    public ConvWriterTxt(Stream stream, Config? cfg = null)
        : base(stream, cfg)
    {
        State = new BufStateTxt(stream, new byte[cfg?.Blocksize ?? Config.Default.Blocksize]);
    }
    public override void WriteSchema(Schema? sch)
    {
        base.WriteSchema(sch);
        if (sch != null)
            State.Enum.Reset(sch.Type);
    }
}
