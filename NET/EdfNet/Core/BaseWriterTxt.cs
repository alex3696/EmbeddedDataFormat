using EdfNet.Core.Text;

namespace EdfNet.Core;

public abstract class BaseWriterTxt : BaseDisposable, IWriter
{
    private readonly byte[] _stringBuf = new byte[4096];
    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;
    protected readonly Stream _st;

    public BaseWriterTxt(Stream stream, EdfConfig? cfg = null)
    {
        Cfg = cfg ?? EdfConfig.Default;
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
    public void WriteConfig(EdfConfig h)
    {
        Flush();
        Write("//Edf Config: VersMajor; VersMinor; Blocksize; Encoding; Flags");
        Write(EdfTokenLiterals.EndLine);
        Write(EdfTokenLiterals.ConfigBegin);
        Write(EdfTokenLiterals.StructBegin);
        Write($"{h.VersMajor};{h.VersMinor};{h.BlockSize};{h.Encoding};{(uint)h.Flags};");
        Write(EdfTokenLiterals.StructEnd);
        Write(EdfTokenLiterals.BlockEnd);
        Write(EdfTokenLiterals.EndLine);
        //Write($"// ? - struct @ - data // - comment");
        CurrentSchema = null;
    }
    public virtual void WriteSchema(EdfSchema? sch)
    {
        CurrentSchema = sch;
        if (null == sch)
            return;
        Flush();
        Write(EdfTokenLiterals.EndLine);
        Write(EdfTokenLiterals.SchemaBegin);
        Write(EdfTokenLiterals.Space);
        Write(EdfTokenLiterals.StructBegin);
        Write($"{sch.Id};\"{sch.Name}\";");
        if (!string.IsNullOrEmpty(sch.Desc))
            Write($"\"{sch.Desc}\";");
        Write(EdfTokenLiterals.StructEnd);
        Write(EdfTokenLiterals.Space);
        ToString(sch.Type);
        Write(EdfTokenLiterals.BlockEnd);
        Write(EdfTokenLiterals.EndLine);
    }
    public virtual EdfErr WriteValue<T>(in T val)
    {
        return EdfErr.WrongType;
    }
    public EdfErr WriteInfData<T>(ushort id, PoType pt, string name, T d)
    {
        WriteSchema(new EdfSchema() { Id = id, Type = new(pt), Name = name, });
        return WriteValue(d);
    }
    protected void Write(ReadOnlySpan<byte> b)
    {
        _st.Write(b);
    }
    protected void Write(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return;
        int len = Encoding.UTF8.GetBytes(str, 0, str.Length, _stringBuf, 0);
        _st.Write(_stringBuf, 0, len);
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
