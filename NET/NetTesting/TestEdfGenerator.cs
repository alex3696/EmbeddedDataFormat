namespace NetTest;

[TestClass]
public class GenSerializationTests
{
    [TestMethod]
    public void Generate_Schema()
    {
        // 1. ACT: Получаем сгенерированную бинарную схему напрямую без создания объектов
        Schema schema = ComplexType.GetEdfSchema();

        // 2. ASSERT: Базовые проверки корневого контейнера схемы
        Assert.IsNotNull(schema, "Схема не должна быть null");
        Assert.AreEqual(0, schema.Id, "Дефолтный Id схемы должен быть равен 0");
        Assert.AreEqual("ComplexTypeSchema", schema.Name);

        Assert.IsNotNull(schema.Type, "Корневой тип схемы не должен быть null");
        Assert.AreEqual(PoType.Struct, schema.Type.Type, "Корневой тип схемы должен быть PoType.Struct");
        Assert.AreEqual("ComplexType", schema.Type.Name);
        Assert.IsNotNull(schema.Type.Childs, "Список дочерних элементов схемы не должен быть null");

        // В классе KeyVal ровно 8 свойств, пригодных для сериализации
        Assert.HasCount(8, schema.Type.Childs, "Количество корневых свойств в схеме должно быть равно 8");

        // 3. ASSERT: Поэлементная проверка плоских примитивов
        // Поле 1: public string? Test1 { get; set; }
        var pTest1 = schema.Type.Childs[0];
        Assert.AreEqual(PoType.String, pTest1.Type);
        Assert.AreEqual("Test1", pTest1.Name);

        // Поле 2: public string? Key { get; set; }
        var pKey = schema.Type.Childs[1];
        Assert.AreEqual(PoType.String, pKey.Type);
        Assert.AreEqual("Key", pKey.Name);

        // Поле 3: public int Val { get; set; }
        var pVal = schema.Type.Childs[2];
        Assert.AreEqual(PoType.Int32, pVal.Type);
        Assert.AreEqual("Val", pVal.Name);

        // Поле 4: public int NVal { get; set; } (присутствует в вашей схеме)
        var pNVal = schema.Type.Childs[3];
        Assert.AreEqual(PoType.Int32, pNVal.Type);
        Assert.AreEqual("NVal", pNVal.Name);

        // 4. ASSERT: Проверка многомерного числового массива int[,,]
        // Поле 5: public int[,,] Arr { get; set; } с атрибутом [3, 2, 1]
        var pArr = schema.Type.Childs[4];
        Assert.AreEqual(PoType.Int32, pArr.Type);
        Assert.AreEqual("Arr", pArr.Name);
        Assert.IsNotNull(pArr.Dims, "Размерности массива Arr не должны быть null");
        Assert.HasCount(3, pArr.Dims, "Массив Arr должен быть трехмерным");
        Assert.AreEqual(3, pArr.Dims[0]);
        Assert.AreEqual(2, pArr.Dims[1]);
        Assert.AreEqual(1, pArr.Dims[2]);

        // 5. ASSERT: Проверка вложенной Nullable-структуры Sub0
        // Поле 6: public SubVal? Sub0 { get; set; }
        var pSub0 = schema.Type.Childs[5];
        Assert.AreEqual(PoType.Struct, pSub0.Type);
        Assert.AreEqual("Sub0", pSub0.Name);
        Assert.IsNotNull(pSub0.Childs, "Внутренние поля Sub0 не должны быть null");
        Assert.HasCount(3, pSub0.Childs, "Структура SubVal должна содержать 3 поля");

        // Проверяем внутренности SubVal внутри Sub0
        Assert.AreEqual(PoType.Double, pSub0.Childs[0].Type); Assert.AreEqual("ValDouble", pSub0.Childs[0].Name);
        Assert.AreEqual(PoType.UInt8, pSub0.Childs[1].Type); Assert.AreEqual("ValByte", pSub0.Childs[1].Name);
        Assert.AreEqual(PoType.Int8, pSub0.Childs[2].Type); Assert.AreEqual("ValSByte", pSub0.Childs[2].Name);

        // Поле 7: public SubVal Sub1 { get; set; } (Обычная структура — внутренности должны совпадать)
        EdfType? pSub1 = schema.Type.Childs[6];
        Assert.IsNotNull(pSub1.Childs, "Внутренние поля Sub1 не должны быть null");
        Assert.AreEqual(PoType.Struct, pSub1.Type);
        Assert.AreEqual("Sub1", pSub1.Name);
        Assert.HasCount(3, pSub1.Childs);

        // 6. ASSERT: Проверка многомерного массива вложенных объектов SubVal[,]
        // Поле 8: public SubVal[,]? Sub { get; set; } с атрибутом [2, 2]
        var pSubArray = schema.Type.Childs[7];
        Assert.AreEqual(PoType.Struct, pSubArray.Type);
        Assert.AreEqual("Sub", pSubArray.Name);

        // Проверяем размерности самого двухмерного массива
        Assert.IsNotNull(pSubArray.Dims, "Размерности массива объектов Sub не должны быть null");
        Assert.HasCount(2, pSubArray.Dims, "Массив Sub должен быть двухмерным");
        Assert.AreEqual(2, pSubArray.Dims[0]);
        Assert.AreEqual(2, pSubArray.Dims[1]);

        // Проверяем, что внутри каждой ячейки массива корректно развернулись поля структуры SubVal
        Assert.IsNotNull(pSubArray.Childs, "Поля структуры внутри массива объектов не должны быть null");
        Assert.HasCount(3, pSubArray.Childs);
        Assert.AreEqual(PoType.Double, pSubArray.Childs[0].Type);
        Assert.AreEqual(PoType.UInt8, pSubArray.Childs[1].Type);
        Assert.AreEqual(PoType.Int8, pSubArray.Childs[2].Type);
    }

    [TestMethod]
    public void KeyVal_Serialization_And_Deserialization_Should_Be_Identical()
    {
        // 1. ARRANGE: Создаем и максимально разнообразно заполняем тестовый объект
        var original = TestClasses_Content.TestValue;

        // Создаем буфер в памяти (имитация файла/флеш-блока)
        using var memoryStream = new MemoryStream(1024);
        using var writer = new EdfNet.Gen.WriterBin(memoryStream);

        // 2. ACT (WRITE): Записываем объект через универсальный метод генерации
        writer.Write(ComplexType.GetEdfSchema());
        EdfErr writeResult = writer.WriteValue(original);
        writer.Flush(); // Сбрасываем остатки буфера в поток

        Assert.AreEqual(EdfErr.IsOk, writeResult);

        // Сбрасываем поток в начало для чтения
        memoryStream.Position = 0;
        var reader = new EdfNet.Gen.ReaderBin(memoryStream);

        // 3. ACT (READ): Читаем объект обратно из бинарного потока
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        reader.ReadSchema();
        reader.ReadBlock();
        ComplexType restored = new();
        var readEnumerator = new ComplexTypeByteEnumerator(restored);
        EdfErr readResult = reader.ReadData(ref readEnumerator);
        Assert.AreEqual(EdfErr.IsOk, readResult);

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
        Assert.IsNotNull(restored.Sub0);
        Assert.AreEqual(original.Sub0?.ValDouble, restored.Sub0.ValDouble);
        Assert.AreEqual(original.Sub0?.ValByte, restored.Sub0.ValByte);
        Assert.AreEqual(original.Sub0?.ValSByte, restored.Sub0.ValSByte);

        // Проверка обычной структуры Sub1
        Assert.AreEqual(original.Sub1.ValDouble, restored.Sub1.ValDouble);
        Assert.AreEqual(original.Sub1.ValByte, restored.Sub1.ValByte);
        Assert.AreEqual(original.Sub1.ValSByte, restored.Sub1.ValSByte);

        // Проверка двухмерного массива объектов SubVal[,]
        Assert.IsNotNull(restored.Sub);
        Assert.AreEqual(original.Sub?.Rank, restored.Sub.Rank);
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                Assert.AreEqual(original.Sub?[i, j].ValDouble, restored.Sub[i, j].ValDouble);
                Assert.AreEqual(original.Sub?[i, j].ValByte, restored.Sub[i, j].ValByte);
                Assert.AreEqual(original.Sub?[i, j].ValSByte, restored.Sub[i, j].ValSByte);
            }
        }
    }

    [TestMethod]
    public void KeyVal_With_Null_Properties_Should_Serialize_Correctly_Or_Handle_Gracefully()
    {
        // Тест пограничного состояния: когда строки и массивы равны null.
        var original = new ComplexType
        {
            Test1 = null,
            Key = null,
            Val = 0,
            Arr = null!,
            Sub0 = null,
            Sub = null
        };

        using var memoryStream = new MemoryStream();
        using var writer = new EdfNet.Gen.WriterBin(memoryStream);
        writer.Write(ComplexType.GetEdfSchema());
        EdfErr writeResult = writer.WriteValue(original);

        var enumerator = (ComplexTypeByteEnumerator)original.GetByteEnumerator();
        writer.WriteEnumerator(ref enumerator);
        writer.WriteValue(original);

        writer.Flush();

        Assert.AreEqual(EdfErr.IsOk, writeResult);
    }

}

