namespace EdfNet.Core;

//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 16, Pack = 1)]

// Code Page Identifiers
// https://learn.microsoft.com/en-us/windows/win32/intl/code-page-identifiers

[Flags]
public enum Options : uint
{
    Default = 0,
};
/*
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct EdfConfig
{
    public EdfConfig()
    { }

    public byte VersMajor = 0x01;
    public byte VersMinor = 0x00;
    public UInt16 Encoding = 65001;
    public UInt16 Blocksize = 256;
    public UInt16 Reserved;
    public Options Flags = Options.Default;
    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    //public string? Description;     //описание файла
    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3000)]
    //public byte[] Data;             //данные динамограммы [3000]

    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    //public byte[] ClusterArray;       //номер куста [6]
    //public string? Cluster
    //{
    //    get => StdTypeExt.StringTool.GetStringUtf8(ClusterArray);
    //    set => StdTypeExt.StringTool.SetStringUtf8(value, ClusterArray);
    //}
}
*/

public class Config : IEquatable<Config>
{
    public const byte Major = 0x00;
    public const byte Minor = 0x03;


    public byte VersMajor = Major;
    public byte VersMinor = Minor;
    public ushort Encoding = 65001;
    public ushort Blocksize = 256;
    public Options Flags = Options.Default;

    public Config()
    {
        //Name = "bdf ".ToCharArray();
    }

    public static readonly Config Default = new();

    public bool Equals(Config? other)
    {
        if (null == other)
            return false;
        if (VersMajor != other.VersMajor) return false;
        if (VersMinor != other.VersMinor) return false;
        if (Encoding != other.Encoding) return false;
        if (Blocksize != other.Blocksize) return false;
        if (Flags != other.Flags) return false;
        return true;
    }
    public override bool Equals(object? obj) => obj is Config header && Equals(header);
    public override int GetHashCode() => throw new NotImplementedException();
}
