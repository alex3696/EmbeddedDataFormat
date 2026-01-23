using NetEdf.StoreTypes;
using NetEdf.Base;
using NetEdf.src;

namespace NetEdf.src;

public class Var
{
    public TypeInfo Info { get; set; }
    public List<byte[]>? Values
    {
        get => _values;
        set
        {
            ClearValues();
            if (value != null)
                for (var i = 0; i < value.Count; i++)
                    AddValue(value[i]);
        }
    }

    public void ClearValues()
    {
        _values = null;
    }

    public Var(TypeInfo inf, List<byte[]>? values)
    {
        Info = inf;
        Values = values;
    }
    public Var(TypeInfo inf, byte[]? value = null)
    {
        Info = inf;
        if (value != null)
            _values = new List<byte[]> { value };
    }

    public void AddValue(byte[] src, int start, int len) => AddValue(src.AsSpan(start, len));
    public void AddValue(byte[] src, int start = 0) => AddValue(src.AsSpan(start));
    public void AddValue(ReadOnlySpan<byte> src)
    {
        if (null == _values)
            _values = [];
        var b = new byte[src.Length];
        src.CopyTo(b);
        _values.Add(b);
    }
    public void AddValue<T>(T val)
        where T : struct
    {
        if (null == _values)
            _values = [];
        _values.Add(StructSerialize.ToBytes(val));
    }

    public static Var Make<T>(string name, T val)
        where T : struct
    {
        switch (val)
        {
            default: break;
            case bool b:
                return new Var(new(name, PoType.UInt8), [(byte)(b ? 1 : 0)]);
            case byte u8:
                return new Var(new(name, PoType.UInt8), [u8]);
            case sbyte i8:
                return new Var(new(name, PoType.Int8), [(byte)i8]);
            case ushort u16:
                return new Var(new(name, PoType.UInt16), BitConverter.GetBytes(u16));
            case short i16:
                return new Var(new(name, PoType.Int16), BitConverter.GetBytes(i16));
            case uint u32:
                return new Var(new(name, PoType.UInt32), BitConverter.GetBytes(u32));
            case int i32:
                return new Var(new(name, PoType.Int32), BitConverter.GetBytes(i32));
            case ulong u64:
                return new Var(new(name, PoType.UInt64), BitConverter.GetBytes(u64));
            case long i64:
                return new Var(new(name, PoType.Int64), BitConverter.GetBytes(i64));
            case Half half:
                return new Var(new(name, PoType.Half), BitConverter.GetBytes(half));
            case float fsingle:
                return new Var(new(name, PoType.Single), BitConverter.GetBytes(fsingle));
            case double fdouble:
                return new Var(new(name, PoType.Double), BitConverter.GetBytes(fdouble));
                //            case string str:
                //                return new Var(new(name, PoType.String), BString.GetBytes(str));
        }
        throw new NotSupportedException();
    }

    public static Var Make(string name, string val)
    {
        return new Var
        (
            inf: new TypeInfo(name, PoType.String),
            value: BString.GetBytes(val)
        );
    }

    public static bool IsEqual(List<byte[]>? x, List<byte[]>? y)
    {
        if (null != x && null != y && x.Count == y.Count)
        {
            for (var i = 0; i < x.Count; ++i)
            {
                var xi = x[i];
                var yi = y[i];
                if (xi is not null && yi is not null)
                    return Enumerable.SequenceEqual(xi, yi);
            }
        }
        return false;
    }

    private List<byte[]>? _values;
}









