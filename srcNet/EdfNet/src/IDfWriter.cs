using NetEdf.Base;

namespace NetEdf.src;

public interface IDfWriter
{
    void Write(Header v);
    void WriteVarInfo(TypeInf t);
    void WriteVarData(ReadOnlySpan<byte> b);
    void Flush();
}

public static class IDfWriterExt
{

}
