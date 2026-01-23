namespace NetEdf.src;

/// <summary>
///  logic block type
/// </summary>
public enum BlockType : byte
{
    Header = 126, //0xB0, ~
    VarInfo = 63, //0x3f, ?
    VarData = 61, //0x3d, =
}
