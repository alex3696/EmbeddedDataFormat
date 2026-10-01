namespace EdfSiam.Common;

[EdfSerializable((ushort)SchemaId.FILETYPEID)]
public class FileTypeId
{
    public ushort Type;
    public ushort Version;
}
