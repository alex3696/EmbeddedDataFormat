namespace EdfSiam.Common;

[EdfSerializable((ushort)SchemaId.DEVICEINFO, "DevInfo")]
public class DeviceInfo
{
    public ushort SwId;
    public ushort SwModel;
    public ushort SwRevision;
    public ushort HwId;
    public ushort HwModel;
    public ulong HwNumber;
}
