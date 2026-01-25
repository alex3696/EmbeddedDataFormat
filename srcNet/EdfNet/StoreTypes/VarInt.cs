using System.Numerics;

namespace NetEdf.StoreTypes;

// https://habr.com/ru/articles/350796/
// https://github.com/protocolbuffers/protobuf/blob/main/csharp/src/Google.Protobuf/WritingPrimitives.cs
// https://github.com/protocolbuffers/protobuf/blob/main/csharp/src/Google.Protobuf/ParsingPrimitives.cs

/// <summary>
///_ ----------------   protobuf    compact<br/>
///1 byte max values:       128                 <br/>
///2 byte max values:     16384 + 1byte values  <br/>
///3 byte max values:   2097152 + 2byte values  <br/>
///4 byte max values: 268435456 + 3byte values  <br/>
/// </summary>
public static class VarInt
{
    public const byte maxVarintBytes = 10;
    public const byte MinUnsigned = 0;
    public const byte MaxUInt1b = 127;
    public const ushort MaxUInt2b = 16511;
    //                     uint32 = 65535
    public const uint MaxUInt3b = 2113663;
    public const uint MaxUInt4b = 270549119;
    //                   uint32 = 4294967295   
    public const ulong MaxUInt5b = 34630287487;
    public const ulong MaxUInt6b = 4432676798591;
    public const ulong MaxUInt7b = 567382630219903;
    public const ulong MaxUInt8b = 72624976668147839;
    public const ulong MaxUInt9b = 9295997013522923647;
    //                     ulong =18446744073709551615
    //public const ulong MaxUInt10b = ;

    public const sbyte MinInt1b = -64;
    public const short MinInt2b = -8256;
    public const int MinInt3b = -1056832;
    public const int MinInt4b = -135274560;
    public const long MinInt5b = -17315143744;
    public const long MinInt6b = -2216338399296;
    public const long MinInt7b = -283691315109952;
    public const long MinInt8b = -36312488334073920;
    public const long MinInt9b = -72341285353037888;

    public const byte MaxInt1b = 63;
    public const ushort MaxInt2b = 8255;
    public const uint MaxInt3b = 1056831;
    public const uint MaxInt4b = 135274559;
    public const ulong MaxInt5b = 17315143743;
    public const ulong MaxInt6b = 2216338399295;
    public const ulong MaxInt7b = 283691315109951;
    public const ulong MaxInt8b = 36312488334073919;
    public const ulong MaxInt9b = 144682570706075774;

    public const int ErrValueOverflow = -2;
    public const int ErrBufOverflow = -1;
    public const int ErrBufTooSmall = -1;
    public static int GetEncSizeT<T>(T x)
    where T : struct, IBinaryInteger<T>, IUnsignedNumber<T>
    {
        int n = 0;
        T mm = T.CreateChecked(127);
        while (mm < x)
        {
            n++;
            x >>= 7;
            x--;
        }
        n++;
        return n;
    }
    public static int EncodeU<T>(T x, Span<byte> buf)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>, IUnsignedNumber<T>
    {
        //if (!T.IsZero(T.MinValue))
        //    x = EncodeZigZag<T>(x);
        int n = 0;
        while (!T.IsZero(x & (~T.CreateChecked(127))))
        {
            if (n >= buf.Length)
                return ErrBufOverflow;
            buf[n++] = (byte)(byte.CreateTruncating(x) & 0x7F | 0x80);
            x >>= 7;
            x--;
        }
        buf[n++] = byte.CreateTruncating(x);
        return n;
    }
    public static int DecodeU<T>(ReadOnlySpan<byte> buf, out T x)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>, IUnsignedNumber<T>
    {
        x = default;
        int n = 0;
        for (int shift = 0; shift < x.GetByteCount() * 8; shift += 7)
        {
            if (n >= buf.Length)
                return ErrBufTooSmall;
            byte b = buf[n++];
            T add = (T.CreateTruncating(b)) << shift;
            if (T.MaxValue - x < add) // check overflow
                return ErrValueOverflow;
            x += add;
            if ((b & 0x80) == 0)
            {
                //if (!T.IsZero(T.MinValue))
                //    x = DecodeZigZag(x);
                return n;
            }
        }
        return -3;
    }

    public static int GetEncSize(sbyte x) => GetEncSize(EncodeZigZag16((short)x));
    public static int GetEncSize(short x) => GetEncSize(EncodeZigZag16(x));
    public static int GetEncSize(int x) => GetEncSizeT(EncodeZigZag32(x));
    public static int GetEncSize(long x) => GetEncSizeT(EncodeZigZag64(x));
    public static int GetEncSize(byte x) => GetEncSizeT((ushort)x);
    public static int GetEncSize(ushort x) => GetEncSizeT(x);
    public static int GetEncSize(uint x) => GetEncSizeT(x);
    public static int GetEncSize(ulong x) => GetEncSizeT(x);

    public static int EncodeUInt64(ulong x, Span<byte> buf) => EncodeU(x, buf);
    public static int EncodeUInt32(uint x, Span<byte> buf) => EncodeU(x, buf);
    public static int EncodeUInt16(ushort x, Span<byte> buf) => EncodeU(x, buf);
    public static int EncodeInt16(short x, Span<byte> buf) => EncodeUInt16(EncodeZigZag16(x), buf);
    public static int EncodeInt32(int x, Span<byte> buf) => EncodeUInt32(EncodeZigZag32(x), buf);
    public static int EncodeInt64(long x, Span<byte> buf) => EncodeUInt64(EncodeZigZag64(x), buf);
    public static byte[] EncodeUInt64(ulong value)
    {
        var b = new byte[maxVarintBytes];
        var dataLen = EncodeUInt64(value, b);
        //return (0 < dataLen) ? b.AsSpan(0, dataLen).ToArray() : [];
        // EncodeUInt64 падает только при недостаточном буфере,
        // буфер тут всегда достаточный - проверка не требуется
        return b.AsSpan(0, dataLen).ToArray();
    }
    public static byte[] EncodeUInt32(uint value)
    {
        var b = new byte[maxVarintBytes];
        var dataLen = EncodeUInt32(value, b);
        return b.AsSpan(0, dataLen).ToArray();
    }
    public static byte[] EncodeUInt16(ushort value)
    {
        var b = new byte[maxVarintBytes];
        var dataLen = EncodeUInt16(value, b);
        return b.AsSpan(0, dataLen).ToArray();
    }
    public static byte[] EncodeInt64(long value) => EncodeUInt64(EncodeZigZag64(value));
    public static byte[] EncodeInt32(int value) => EncodeUInt32(EncodeZigZag32(value));
    public static byte[] EncodeInt16(short value) => EncodeUInt16(EncodeZigZag16(value));

    public static int DecodeUInt64(ReadOnlySpan<byte> buf, out ulong x) => DecodeU(buf, out x);
    public static int DecodeUInt32(ReadOnlySpan<byte> buf, out uint x) => DecodeU(buf, out x);
    public static int DecodeUInt16(ReadOnlySpan<byte> buf, out ushort x) => DecodeU(buf, out x);
    public static int DecodeInt64(ReadOnlySpan<byte> buffer, out long result)
    {
        int b = DecodeUInt64(buffer, out ulong r);
        result = 0 < b ? DecodeZigZag64(r) : default;
        return b;
    }
    public static int DecodeInt32(ReadOnlySpan<byte> buffer, out int result)
    {
        int b = DecodeUInt32(buffer, out uint r);
        result = 0 < b ? DecodeZigZag32(r) : default;
        return b;
    }
    public static int DecodeInt16(ReadOnlySpan<byte> buffer, out short result)
    {
        int b = DecodeUInt16(buffer, out ushort r);
        result = 0 < b ? DecodeZigZag16(r) : default;
        return b;
    }

    public static ushort EncodeZigZag16(short n) => (ushort)(n << 1 ^ n >> 15);
    public static short DecodeZigZag16(ushort n) => (short)(n >> 1 ^ -(short)(n & 1));
    public static uint EncodeZigZag32(int n) => (uint)(n << 1 ^ n >> 31);
    public static int DecodeZigZag32(uint n) => (int)(n >> 1) ^ -(int)(n & 1);
    public static ulong EncodeZigZag64(long n) => (ulong)(n << 1 ^ n >> 63);
    public static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -(long)(n & 1);
    // не работает, т.к. бинарное представление для 16 бит всегда неявно приводится к 32
    // -32768(0xFFFF) становится 0xFFFF8000
    //public static T EncodeZigZag<T>(T x)
    //    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>, INumber<T>
    //{
    //    return x << 1 ^ x >> (x.GetByteCount() * 8 - 1);
    //}
    //public static T DecodeZigZag<T>(T x) where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    //{
    //    return T.CreateChecked(x >> 1) ^ -(x & T.One);
    //}

    #region oveloaded  functions
    public static int Encode(ushort value, Span<byte> buf) => EncodeU(value, buf);
    public static int Encode(uint value, Span<byte> buf) => EncodeU(value, buf);
    public static int Encode(ulong value, Span<byte> buf) => EncodeU(value, buf);
    public static int Encode(short value, Span<byte> buf) => EncodeU(EncodeZigZag16(value), buf);
    public static int Encode(int value, Span<byte> buf) => EncodeU(EncodeZigZag32(value), buf);
    public static int Encode(long value, Span<byte> buf) => EncodeU(EncodeZigZag64(value), buf);
    public static int Decode(ReadOnlySpan<byte> buf, out ushort x) => DecodeU(buf, out x);
    public static int Decode(ReadOnlySpan<byte> buf, out uint x) => DecodeU(buf, out x);
    public static int Decode(ReadOnlySpan<byte> buf, out ulong x) => DecodeU(buf, out x);
    public static int Decode(ReadOnlySpan<byte> buf, out short x) => DecodeInt16(buf, out x);
    public static int Decode(ReadOnlySpan<byte> buf, out int x) => DecodeInt32(buf, out x);
    public static int Decode(ReadOnlySpan<byte> buf, out long x) => DecodeInt64(buf, out x);
    #endregion
}
