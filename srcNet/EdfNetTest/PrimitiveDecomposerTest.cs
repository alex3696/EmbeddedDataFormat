using NetEdf.src;

namespace NetEdfTest;

[TestClass]
public class PrimitiveDecomposerTest
{
    [TestMethod]
    public void DecomposeSimpleTypeTest()
    {
        int num = 12365;

        var decomposer = new PrimitiveDecomposer(num).ToArray();

        Assert.AreEqual(12365, decomposer[0]);
    }

    [TestMethod]
    public void DecomposeCollectionTypeTest()
    {
        int[] nums = { 1, 2, 3, 4 };

        var decomposer = new PrimitiveDecomposer(nums).ToArray();
        Assert.AreEqual((int)1, decomposer[0]);
        Assert.AreEqual((int)2, decomposer[1]);
        Assert.AreEqual((int)3, decomposer[2]);
        Assert.AreEqual((int)4, decomposer[3]);
    }

    [TestMethod]
    public void DecomposeDifficultTypeTest()
    {
        var playerInfo = new
        {
            Name = "Player",
            Health = 100,
            Level = 25,
            SkillPoints = 2,
            CountAchievements = 35

        };

        var decomposer = new PrimitiveDecomposer(playerInfo).ToArray();

        Assert.AreEqual("Player", decomposer[0]);
        Assert.AreEqual(100, decomposer[1]);
        Assert.AreEqual(25, decomposer[2]);
        Assert.AreEqual(2, decomposer[3]);
        Assert.AreEqual(35, decomposer[4]);
    }
}
