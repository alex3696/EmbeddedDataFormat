namespace EdfNet.Core;

public class EdfSchema
{
    public ushort Id; // var id
    public string? Name; // var name
    public string? Desc; // var description
    public required EdfType Type; // var type
}
