namespace EdfNet.Core.Text;

public class BufStateTxt
{
    public readonly Stream Stream;
    public readonly byte[] _Buf;
    public readonly CircularEdfTypeEnumeratorTxt Enum = new();

    public Span<byte> Buf => _Buf;
    public int Writed;   // сколько байт реально загружено в Buf из Stream

    public BufStateTxt(Stream stream, byte[] buf)
    {
        Stream = stream;
        _Buf = buf;
        //Readed = 0;
        Writed = 0;
    }
}
