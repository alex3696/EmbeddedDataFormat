namespace EdfSiam.Common;

[EdfSerializable((ushort)SchemaId.POSITION, "Position")]
public class Position
{
    public string? Field;
    public string? Cluster;
    public string? Well;
    public string? Shop;
}
