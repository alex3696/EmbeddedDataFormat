using EdfSchema = EdfNet.Core.Schema;

namespace NetTest;

[TestClass]
public class EdfTypeCreator_Test
{
    [TestMethod]
    public void Equal_EdfTypes()
    {
        EdfType test = TestClasses_Content.KeyValSchema.Type;

        EdfType genSch = GetGenSchema().Type;
        Assert.IsTrue(genSch.Equals(test));
    }

    public EdfSchema GetGenSchema()
    {
        return NetTest.ComplexType.GetEdfSchema();
    }
}
