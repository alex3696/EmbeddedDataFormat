namespace EdfNet.Core;

public abstract class BaseWriterTxt : BaseDisposable, IWriter
{
    public Config Cfg { get; }
    public Schema? CurrentSchema;
    protected readonly Stream _st;

    public BaseWriterTxt(Stream stream, Config? cfg = null)
    {
        Cfg = cfg ?? Config.Default;
        _st = stream;
        if (0 == stream.Position)
            WriteConfig(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _st.Flush();
        base.Dispose(disposing);
    }
    public void Flush()
    {
        _st.Flush();
    }
    public void WriteConfig(Config h)
    {
        Flush();
        Write($"<~ {{version={h.VersMajor}.{h.VersMinor}; bs={h.Blocksize}; encoding={h.Encoding}; flags={(uint)h.Flags}; }} >\n");
        //Write($"// ? - struct @ - data // - comment");
        CurrentSchema = null;
    }
    public void WriteSchema(Schema sch)
    {
        Flush();
        Write($"\n\n<? {{");
        Write($"{sch.Id};\"{sch.Name}\"");
        if (!string.IsNullOrEmpty(sch.Desc))
            Write($";\"{sch.Desc}\"");
        Write($"}} ");
        ToString(sch.Type);
        Write($">");
        CurrentSchema = sch;
    }
    public virtual EdfErr WriteValue<T>(in T val)
    {
        return EdfErr.WrongType;
    }
    public EdfErr WriteInfData<T>(ushort id, PoType pt, string name, T d)
    {
        WriteSchema(new Schema() { Id = id, Type = new(pt), Name = name, });
        return WriteValue(d);
    }
    protected void Write(string? str)
    {
        if (!string.IsNullOrEmpty(str))
            _st.Write(Encoding.UTF8.GetBytes(str));
    }
    protected void ToString(EdfType t, int noffset = 0)
    {
        string offset = GetOffset(noffset);
        Write(offset);
        Write(t.Type.ToString());
        if (null != t.Dims)
        {
            foreach (var d in t.Dims)
                Write($"[{d}]");
        }
        if (!string.IsNullOrEmpty(t.Name))
            Write($" \"{t.Name}\"");
        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            Write($"\n{offset}{{");
            foreach (var it in t.Childs)
            {
                Write($"\n");
                ToString(it, noffset + 1);
            }
            Write($"\n{offset}}}");
        }
        else
            Write(";");
    }
    protected static string GetOffset(int noffset)
    {
        string offset = "";
        for (int i = 0; i < noffset; i++)
            offset += "  ";
        return offset;
    }


}
