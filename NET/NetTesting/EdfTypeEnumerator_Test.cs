using System.Collections.Generic;
using EdfSchema = EdfNet.Core.Schema;

namespace NetTest;

public readonly ref struct Recursive : IPrimitiveIo
{
    #region Unused
    public readonly void SepRecBegin() { }
    public readonly void SepRecEnd() { }
    public readonly void SepBeginStruct() { }
    public readonly void SepEndStruct() { }
    public readonly void SepBeginArray() { }
    public readonly void SepEndArray() { }
    public readonly void SepVarEnd() { }
    #endregion
    public readonly List<EdfType> Result;
    public Recursive(List<EdfType> res)
    {
        Result = res;
    }
    public void Primitive(EdfType edfType)
    {
        Result.Add(edfType);
    }
}
public class RecursiveClass : IPrimitiveIo
{
    #region Unused
    public void SepRecBegin() { }
    public void SepRecEnd() { }
    public void SepBeginStruct() { }
    public void SepEndStruct() { }
    public void SepBeginArray() { }
    public void SepEndArray() { }
    public void SepVarEnd() { }
    #endregion
    public readonly List<EdfType> Result;
    public RecursiveClass(List<EdfType> res)
    {
        Result = res;
    }
    public void Primitive(EdfType edfType)
    {
        Result.Add(edfType);
    }
}

[TestClass]
public class EdfTypeEnumerator_Test
{
    private readonly EdfType[] _stack = new EdfType[256];
    private readonly List<EdfType> _lst = new(100);
    private readonly EdfSchema _schema;
    private readonly RecursiveClass _rcls;

    public EdfTypeEnumerator_Test()
    {
        _schema = NetTest.KeyVal.GetEdfSchema();
        _rcls = new(_lst);
    }

    [TestMethod]
    public void Equal_EdfTypeEnumerators()
    {
        EdfTypeEnumeratorStack();
        List<EdfType> lst = _lst.ToList();

        EdfTypeEnumeratorYield();
        Assert.IsTrue(lst.SequenceEqual(_lst), "EdfTypeEnumeratorYield");

        EdfTypeEnumeratorRecursive();
        Assert.IsTrue(lst.SequenceEqual(_lst), "EdfTypeEnumeratorRecursive");

        EdfTypeEnumeratorRecursiveClass();
        Assert.IsTrue(lst.SequenceEqual(_lst), "EdfTypeEnumeratorRecursiveClass");
    }

    public void EdfTypeEnumeratorStack()
    {
        _lst.Clear();
        foreach (var field in _schema.Type.EnumerateStack(_stack))
        {
            if (null != field)
                _lst.Add(field);
        }
    }
    public void EdfTypeEnumeratorYield()
    {
        _lst.Clear();
        foreach (var field in _schema.Type.EnumerateYield())
        {
            if (null != field)
                _lst.Add(field);
        }
    }
    public void EdfTypeEnumeratorRecursive()
    {
        _lst.Clear();
        var enm = new Recursive(_lst);
        EdfTypeWalkerBin.Process(_schema.Type, ref enm);
    }
    public void EdfTypeEnumeratorRecursiveClass()
    {
        _lst.Clear();
        EdfTypeWalkerBin.Process(_schema.Type, _rcls);
    }
}
