using System.Runtime.InteropServices;

namespace NetTest;

[EdfBinSerializable]
public partial struct SubVal
{
    public SubVal()
    {
    }
    public double ValDouble { get; set; } = 0x11;
    public byte ValByte { get; set; } = 0x22;
    public sbyte ValSByte { get; set; } = 0x33;
}
[EdfBinSerializable]
public partial class KeyVal
{
    public string? Test1 { get; set; }
    public string? Test2 { get; set; }
    public int Key { get; set; }
    public int Val { get; set; }

    public SubVal Sub1 { get; set; }


    [EdfBinArray([2, 2])]
    public SubVal[,]? Sub { get; set; }

    [EdfBinArray([3, 2, 1])]
    public int[,,] Arr { get; set; } = { { { 1, 2 }, { 3, 4 }, { 5, 6 } } };
}

[EdfBinSerializable]
public partial struct TestSignalBlock
{
    public byte Id { get; set; }

    public string? Name { get; set; }

    // Фиксированная матрица 2x3. Будет занимать ровно 2 * 3 * 2 (размер short) = 12 байт
    [EdfBinArray(2, 3)]
    public short[,] Matrix { get; set; }

    public string? Description { get; set; }
}

[TestClass]
public class TestEdfGenerator
{
    [TestMethod]
    public void TestSourceGenSerialize()
    {
        KeyVal kvs = new() { Key = 0xFABC, Val = 0x1234, Test1 = "123", Test2 = "456", Sub = new SubVal[2,2] };
        Span<byte> sa = stackalloc byte[1024];
        kvs.SerializeBin(sa);
        int bc = KeyVal.DeserializeBin(sa, out var okv);

        var sub = new SubVal();
        sub.SerializeBin(sa);

    }

    [TestMethod]
    public void Test_Serialize_And_Deserialize_ShouldMatch()
    {
        // 1. Создаем тестовые данные
        var original = new TestSignalBlock
        {
            Id = 42,
            Name = "ЭЭГ-Сигнал",
            // Создаем матрицу чуть меньшего размера (2x2 вместо 2x3), 
            // чтобы проверить автоматическое заполнение нулями оставшейся части!
            Matrix = new short[2, 2]
            {
                { 10, 20 },
                { 30, 40 }
            },
            Description = "Тест генератора"
        };

        // 2. Проверяем расчет размера
        int requiredSize = original.GetSize();

        // Ожидаемый размер: 
        // 1 (Id) + 22 (Name: 1 байт длина + 21 байт UTF-8) + 12 (Matrix: 2*3*2 байта) + 31 (Description) = 66 байт
        Assert.IsGreaterThan(0, requiredSize, "Размер должен быть больше нуля");

        // 3. Выделяем буфер и сериализуем
        Span<byte> buffer = new byte[requiredSize];
        int bytesWritten = original.SerializeBin(buffer);

        Assert.AreEqual(requiredSize, bytesWritten, "Количество записанных байт должно совпадать с GetSize()");

        // 4. Десериализуем обратно
        int bytesRead = TestSignalBlock.DeserializeBin(buffer, out var des);

        // 5. Проверяем результаты
        Assert.AreEqual(bytesWritten, bytesRead, "Количество прочитанных байт должно совпадать с записанными");
        //Assert.IsNotNull(des);
        var deserialized = des;

        // Проверяем простые свойства
        Assert.AreEqual(original.Id, deserialized.Id);
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.AreEqual(original.Description, deserialized.Description);

        // Проверяем матрицу
        Assert.IsNotNull(deserialized.Matrix);
        // Матрица на выходе должна быть строго целевого размера 2x3 из атрибута [EdfBinArray(2, 3)]
        Assert.AreEqual(2, deserialized.Matrix.GetLength(0));
        Assert.AreEqual(3, deserialized.Matrix.GetLength(1));

        // Проверяем скопированные данные
        Assert.AreEqual(10, deserialized.Matrix[0, 0]);
        Assert.AreEqual(20, deserialized.Matrix[0, 1]);
        Assert.AreEqual(30, deserialized.Matrix[1, 0]);
        Assert.AreEqual(40, deserialized.Matrix[1, 1]);

        // Проверяем, что недостающая колонка (индекс 2) автоматически заполнилась нулями!
        Assert.AreEqual(0, deserialized.Matrix[0, 2]);
        Assert.AreEqual(0, deserialized.Matrix[1, 2]);
        /*
        ReadOnlySpan<byte> source  = new byte[100];
        int offset = 0;

        var srcSlice = source.Slice(offset, 12);
        offset += 12;
        var arr = new short[2, 3];
        ref byte dstRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(arr);
        Span<byte> dstSpan = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref dstRef, 12);
        srcSlice.CopyTo(dstSpan);
        obj.Matrix = arr;
        */
    }


}

