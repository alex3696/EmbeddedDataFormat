using EdfNet.Ref;
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

        EdfType refSch = GetReflSchema().Type;
        Assert.IsTrue(refSch.Equals(test));
    }

    public EdfSchema GetGenSchema()
    {
        return NetTest.KeyVal.GetEdfSchema();
    }

    public EdfSchema GetReflSchema()
    {
        return NetTest.KeyVal.GetEdfSchemaRefl();
    }

}
