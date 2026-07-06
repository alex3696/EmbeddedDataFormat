namespace NetTest;


public struct PlainStruct
{
    public int Val;
}

[EdfSerializable]
public partial struct SubVal
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
    public PlainStruct NotUsed { get; set; } // not used in serialization
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
    public SubVal Sub1 { get; set; }
    [EdfArray([2, 2])]
    public SubVal[,]? Sub { get; set; } = new SubVal[2, 2];
}

[TestClass]
public class EdfBinarySerializationTests
{
    [TestMethod]
    public void KeyVal_Serialization_And_Deserialization_Should_Be_Identical()
    {
        // 1. ARRANGE: Создаем и максимально разнообразно заполняем тестовый объект
        var original = new KeyVal
        {
            Test1 = "Первая тестовая строка с UTF-8 символами №123",
            Key = "Уникальный Ключ Скважины А-40",
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
        var sch = new Schema()
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

        // Создаем буфер в памяти (имитация файла/флеш-блока)
        using var memoryStream = new MemoryStream(1024);
        var writer = new BinWriter(memoryStream);

        // 2. ACT (WRITE): Записываем объект через универсальный метод генерации
        writer.Write(sch);
        EdfErr writeResult = writer.WriteGen(original);
        writer.Flush(); // Сбрасываем остатки буфера в поток

        Assert.AreEqual(EdfErr.IsOk, writeResult);

        // Сбрасываем поток в начало для чтения
        memoryStream.Position = 0;
        var reader = new BinReader(memoryStream);

        // 3. ACT (READ): Читаем объект обратно из бинарного потока
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        reader.ReadSchema();
        var readEnumerator = new KeyValByteEnumerator(new KeyVal());
        EdfErr readResult = reader.ReadData(ref readEnumerator);

        Assert.AreEqual(EdfErr.IsOk, readResult);
        KeyVal restored = readEnumerator.Result; // Забираем готовый объект из энумератора

        // 4. ASSERT: Проверяем идентичность всех полей (в MSTest сначала идет Expected, потом Actual)
        Assert.IsNotNull(restored);
        Assert.AreEqual(original.Test1, restored.Test1);
        Assert.AreEqual(original.Key, restored.Key);
        Assert.AreEqual(original.Val, restored.Val);
        Assert.AreEqual(original.NVal, restored.NVal);

        // Проверка трехмерного числового массива int[,,]
        Assert.IsNotNull(restored.Arr);
        Assert.AreEqual(original.Arr.Rank, restored.Arr.Rank);
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
                for (int k = 0; k < 1; k++)
                    Assert.AreEqual(original.Arr[i, j, k], restored.Arr[i, j, k]);

        // Проверка вложенной Nullable-структуры Sub0
        Assert.IsTrue(restored.Sub0.HasValue);
        Assert.AreEqual(original.Sub0.Value.ValDouble, restored.Sub0.Value.ValDouble);
        Assert.AreEqual(original.Sub0.Value.ValByte, restored.Sub0.Value.ValByte);
        Assert.AreEqual(original.Sub0.Value.ValSByte, restored.Sub0.Value.ValSByte);

        // Проверка обычной структуры Sub1
        Assert.AreEqual(original.Sub1.ValDouble, restored.Sub1.ValDouble);
        Assert.AreEqual(original.Sub1.ValByte, restored.Sub1.ValByte);
        Assert.AreEqual(original.Sub1.ValSByte, restored.Sub1.ValSByte);

        // Проверка двухмерного массива объектов SubVal[,]
        Assert.IsNotNull(restored.Sub);
        Assert.AreEqual(original.Sub.Rank, restored.Sub.Rank);
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                Assert.AreEqual(original.Sub[i, j].ValDouble, restored.Sub[i, j].ValDouble);
                Assert.AreEqual(original.Sub[i, j].ValByte, restored.Sub[i, j].ValByte);
                Assert.AreEqual(original.Sub[i, j].ValSByte, restored.Sub[i, j].ValSByte);
            }
        }
    }

    [TestMethod]
    public void KeyVal_With_Null_Properties_Should_Serialize_Correctly_Or_Handle_Gracefully()
    {
        // Тест пограничного состояния: когда строки и массивы равны null.
        var original = new KeyVal
        {
            Test1 = null,
            Key = null,
            Val = 0,
            Arr = null,
            Sub0 = null,
            Sub = null
        };

        using var memoryStream = new MemoryStream();
        var writer = new BinWriter(memoryStream);

        EdfErr writeResult = writer.WriteGen(original);
        writer.Flush();

        Assert.AreEqual(EdfErr.IsOk, writeResult);
    }

}

