using EdfNet.Interfaces;

namespace NetTest;

public struct PlainStruct
{
    public int Val;
}

[EdfSerializable]
public partial class SubVal
{
    public SubVal()
    {
    }
    public double ValDouble { get; set; } = 0x11;
    public byte ValByte { get; set; } = 0x22;
    public sbyte ValSByte { get; set; } = 0x33;
}
[EdfSerializable]
public partial class KeyVal
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
}

public class TestClasses_Content
{
    public static readonly Schema KeyValSchema = new()
    {
        Id = 0,
        Name = "KeyValSchema",
        Desc = "Schema for KeyVal class",
        Type = new()
        {
            Type = PoType.Struct,
            Name = "KeyVal",
            Childs =
        [
            new (PoType.String, "Test1"),
                new (PoType.String, "Key"),
                new (PoType.Int32, "Val"),
                new (PoType.Int32, "NVal"),
                new (PoType.Int32, "Arr", [3, 2, 1]),
                new (PoType.Struct, "Sub0")
                {
                    Childs =
                    [
                        new (PoType.Double, "ValDouble"),
                        new (PoType.UInt8, "ValByte"),
                        new (PoType.Int8, "ValSByte"),
                    ]
                },
                new (PoType.Struct, "Sub1")
                {
                    Childs =
                    [
                        new (PoType.Double, "ValDouble"),
                        new (PoType.UInt8, "ValByte"),
                        new (PoType.Int8, "ValSByte"),
                    ]
                },
                new (PoType.Struct, "Sub", [2, 2])
                {
                    Childs =
                    [
                        new (PoType.Double, "ValDouble"),
                        new (PoType.UInt8, "ValByte"),
                        new (PoType.Int8, "ValSByte"),
                    ]
                }
        ]
        }
    };


    public static readonly KeyVal TestValue = new()
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
}
