using NetEdf.src;

namespace NetEdfTest;

[TestClass]
public class TestTypeInf
{
    [TestMethod]
    public void TestEquals()
    {
        TypeInf ti1 = new TypeInf()
        {
            Type = PoType.Struct,
            Name = "Struct",
            Dims = [2],
            Childs =
            [
                new (PoType.String, "Key"),
                new (PoType.String, "Value"),
                new (PoType.UInt8, "Test", [3]),
            ]
        };

        TypeInf ti2 = new TypeInf()
        {
            Type = PoType.Struct,
            Name = "Struct",
            Dims = [2],
            Childs =
            [
                new (PoType.String, "Key"),
                new (PoType.String, "Value"),
                new (PoType.UInt8, "Test", [3]),
            ]
        };

        TypeInf ti3 = new TypeInf()
        {
            Type = PoType.Struct,
            Name = "Struct",
            Dims = [2],
            Childs =
            [
                new (PoType.String, "Key"),
                new (PoType.String, "Value"),
            ]
        };
        Assert.IsTrue(ti1.Equals(ti2));
        Assert.IsFalse(ti2.Equals(ti3));
    }

    [TestMethod]
    public void TestGetTotalElements()
    {
        TypeInf ti1 = new TypeInf()
        {
            Type = PoType.Struct,
            Name = "Struct",
            Dims = [5, 5, 5, 5],
            Childs =
            [
                new (PoType.String, "Key"),
                new (PoType.String, "Value"),
                new (PoType.UInt8, "Test", [3]),
            ]
        };

        var actual = ti1.GetTotalElements();

        Assert.AreEqual((uint)625, actual);
    }

}
