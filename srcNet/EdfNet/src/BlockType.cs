namespace NetEdf.src;

/// <summary>
///  logic block type
/// </summary>
public enum BlockType : byte
{
    Config = 126, //0xB0, ~
    Schema = 63, //0x3f, ?
    Data = 61, //0x3d, =
}
