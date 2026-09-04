using EdfNet.Core.Binary;
using EdfNet.Extensions;
using EdfNet.Interfaces;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTest;

public struct PlainStruct
{
    public int Val;
}

[EdfSerializable]
public class SubVal : IEquatable<SubVal>
{
    public SubVal()
    {
    }
    public double ValDouble { get; set; } = 0x11;
    public byte ValByte { get; set; } = 0x22;
    public sbyte ValSByte { get; set; } = 0x33;

    public bool Equals(SubVal? other)
    {
        if (other == null) return false;
        if (other.ValDouble != ValDouble) return false;
        if (other.ValByte != ValByte) return false;
        if (other.ValSByte != ValSByte) return false;
        return true;
    }
}
[EdfSerializable(0, "ComplexTypeSchema")]
public class ComplexType : IEquatable<ComplexType>
{
    //public PlainStruct NotUsed { get; set; } // not used in serialization
    public string? Test1 { get; set; }
    public string? Key { get; set; }
    public int Val { get; set; }
    public int? NVal { get; set; }
    [EdfArray([3, 2, 1])]
    public int[,,] Arr { get; set; } =
        {
            {{ 1 }, { 2 }},
            {{ 3 }, { 4 }},
            {{ 5 }, { 6 }}
        };
    public SubVal? Sub0 { get; set; }
    public SubVal Sub1 { get; set; } = new();
    [EdfArray([2, 2])]
    public SubVal[,]? Sub { get; set; } = new SubVal[2, 2];

    public bool Equals(ComplexType? other)
    {
        if (other == null) return false;
        if (other.Test1 != Test1) return false;
        if (other.Key != Key) return false;
        if (other.Val != Val) return false;
        if (other.NVal != NVal) return false;
        if (other.Arr.Rank != Arr.Rank || other.Arr.Length != Arr.Length) return false;
        for (int i = 0; i < Arr.Length; i++)
        {
            ref var otherElement = ref other.Arr.GetElementAtFlatIndex<int>(i);
            ref var thisElement = ref Arr.GetElementAtFlatIndex<int>(i);
            if (otherElement != thisElement)
                return false;
        }
        if (!Sub1.Equals(other.Sub1)) return false;

        if (Sub == null) return false;
        if (other.Sub == null) return false;
        if (other.Sub.Rank != Sub.Rank || other.Sub.Length != Sub?.Length) return false;
        for (int i = 0; i < Sub.Length; i++)
        {
            ref var otherElement = ref other.Sub.GetElementAtFlatIndex<SubVal>(i);
            ref var thisElement = ref Sub.GetElementAtFlatIndex<SubVal>(i);
            if (!otherElement.Equals(thisElement)) return false;
        }
        return true;
    }
}

public static class TestClasses_Content
{
    public static readonly EdfSchema KeyValSchema = new()
    {
        Id = 0,
        Name = "KeyValSchema",
        Desc = "Schema for KeyVal class",
        Type = new()
        {
            Type = EdfPrimitiveType.Struct,
            Name = "ComplexType",
            Childs =
        [
            new (EdfPrimitiveType.String, "Test1"),
                new (EdfPrimitiveType.String, "Key"),
                new (EdfPrimitiveType.Int32, "Val"),
                new (EdfPrimitiveType.Int32, "NVal"),
                new (EdfPrimitiveType.Int32, "Arr", [3, 2, 1]),
                new (EdfPrimitiveType.Struct, "Sub0")
                {
                    Childs =
                    [
                        new (EdfPrimitiveType.Double, "ValDouble"),
                        new (EdfPrimitiveType.UInt8, "ValByte"),
                        new (EdfPrimitiveType.Int8, "ValSByte"),
                    ]
                },
                new (EdfPrimitiveType.Struct, "Sub1")
                {
                    Childs =
                    [
                        new (EdfPrimitiveType.Double, "ValDouble"),
                        new (EdfPrimitiveType.UInt8, "ValByte"),
                        new (EdfPrimitiveType.Int8, "ValSByte"),
                    ]
                },
                new (EdfPrimitiveType.Struct, "Sub", [2, 2])
                {
                    Childs =
                    [
                        new (EdfPrimitiveType.Double, "ValDouble"),
                        new (EdfPrimitiveType.UInt8, "ValByte"),
                        new (EdfPrimitiveType.Int8, "ValSByte"),
                    ]
                }
        ]
        }
    };


    public static readonly ComplexType TestValue = new()
    {
        Test1 = "Первая тестовая строка с UTF-8 символами №123",
        Key = "Уникальный Ключ А-40",
        Val = 987654321,
        NVal = 1230456,

        // Трехмерный массив примитивов int[,,] размерностью 3х2х1
        Arr = new int[3, 2, 1]
        {
            { { 10 }, { 20 } },
            { { 30 }, { 40 } },
            { { 50 }, { 60 } }
        },

        // Вложенная Nullable-структура (значение заполнено)
        Sub0 = new SubVal { ValDouble = 3.14159, ValByte = 0xAA, ValSByte = -50 },

        // Обычная вложенная структура
        Sub1 = new SubVal { ValDouble = 2.71828, ValByte = 0x55, ValSByte = 120 },

        // Двухмерный массив вложенных объектов SubVal[,] размерностью 2х2
        Sub = new SubVal[2, 2]
        {
            {
                new SubVal { ValDouble = 1.1, ValByte = 11, ValSByte = 12 },
                new SubVal { ValDouble = 1.2, ValByte = 13, ValSByte = 14 }
            },
            {
                new SubVal { ValDouble = 2.1, ValByte = 21, ValSByte = 22 },
                new SubVal { ValDouble = 2.2, ValByte = 23, ValSByte = 24 }
            }
        }
    };


    public static int TryWrite<T>(Span<byte> dst, T val)
    where T : struct
    {
        var len = Unsafe.SizeOf<T>();
        if (dst.Length < len)
            return -1; //throw new EdfDstBufOverflowException();
        MemoryMarshal.Write(dst, val);
        return len;
    }

    static TestClasses_Content()
    {
        Span<byte> dst = new byte[1000];
        int n = 0;
        //SpanBufferWriter sb = new SpanBufferWriter(dst);
        n += EdfBinString.WriteBin(TestValue.Test1, dst.Slice(n));
        n += EdfBinString.WriteBin(TestValue.Key, dst.Slice(n));

        n += TryWrite(dst.Slice(n), TestValue.Val);
        n += TryWrite(dst.Slice(n), TestValue.NVal.Value);

        n += TryWrite(dst.Slice(n), TestValue.Arr[0, 0, 0]);
        n += TryWrite(dst.Slice(n), TestValue.Arr[0, 1, 0]);
        n += TryWrite(dst.Slice(n), TestValue.Arr[1, 0, 0]);
        n += TryWrite(dst.Slice(n), TestValue.Arr[1, 1, 0]);
        n += TryWrite(dst.Slice(n), TestValue.Arr[2, 0, 0]);
        n += TryWrite(dst.Slice(n), TestValue.Arr[2, 1, 0]);

        n += TryWrite(dst.Slice(n), TestValue.Sub0.ValDouble);
        n += TryWrite(dst.Slice(n), TestValue.Sub0.ValByte);
        n += TryWrite(dst.Slice(n), TestValue.Sub0.ValSByte);

        n += TryWrite(dst.Slice(n), TestValue.Sub1.ValDouble);
        n += TryWrite(dst.Slice(n), TestValue.Sub1.ValByte);
        n += TryWrite(dst.Slice(n), TestValue.Sub1.ValSByte);

        n += TryWrite(dst.Slice(n), TestValue.Sub[0, 0].ValDouble);
        n += TryWrite(dst.Slice(n), TestValue.Sub[0, 0].ValByte);
        n += TryWrite(dst.Slice(n), TestValue.Sub[0, 0].ValSByte);

        n += TryWrite(dst.Slice(n), TestValue.Sub[0, 1].ValDouble);
        n += TryWrite(dst.Slice(n), TestValue.Sub[0, 1].ValByte);
        n += TryWrite(dst.Slice(n), TestValue.Sub[0, 1].ValSByte);

        n += TryWrite(dst.Slice(n), TestValue.Sub[1, 0].ValDouble);
        n += TryWrite(dst.Slice(n), TestValue.Sub[1, 0].ValByte);
        n += TryWrite(dst.Slice(n), TestValue.Sub[1, 0].ValSByte);

        n += TryWrite(dst.Slice(n), TestValue.Sub[1, 1].ValDouble);
        n += TryWrite(dst.Slice(n), TestValue.Sub[1, 1].ValByte);
        n += TryWrite(dst.Slice(n), TestValue.Sub[1, 1].ValSByte);

        TestValueBin = dst.Slice(0, n).ToArray();
    }
    public static readonly byte[] TestValueBin;
}
