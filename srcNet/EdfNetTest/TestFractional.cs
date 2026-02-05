using NetEdf.src;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetEdfTest;

[TestClass]
public class TestFractional
{
    [TestMethod]
    public void Test_FpFloat_In_Out()
    {
        double pi = 1.23;
        var ff = FpFloat.Parse(pi);
        Assert.AreEqual(pi, ff.ToDouble());
    }

    [TestMethod]
    public void Test_Fractional_In_Out()
    {
        double pi = 3.145;

        var ff = Fraction.Parse(pi);

        Assert.AreEqual(pi, ff.ToDouble(10));

        double n = 0.1;

        var fn = Fraction.Parse(n);

        Assert.AreEqual(n, fn.ToDouble(10));
    }

}
