using EdfNet.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
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


[EdfSerializable]
public struct Shape
{
    [EdfArray(1000)]
    public Point[] Path;
}
[EdfSerializable]
public struct Point
{
    public int X;
    public int Y;
}


[TestClass]
public class EdfTypeEnumerator_Test
{
    [DebuggerDisplay("{DebugString(),nq}")]
    struct StackItem
    {
        public Token Token;
        public EdfType? Type;
        public string DebugString()
        {
            return $"{Token} : {Type?.DebugString()}";
        }
    }

    private readonly EdfType[] _stack = new EdfType[256];
    private readonly List<EdfType> _lst = new(100);
    private readonly StackItem[] _lstToken = new StackItem[4096];
    private int _lstTokenCount = 0;

    private readonly EdfSchema _schema;
    private readonly RecursiveClass _rcls;
    public EdfTypeEnumeratorStackInlineArray _enm;
    public EdfTypeEnumeratorToken _enmToken = new();

    public EdfTypeEnumerator_Test()
    {
        _schema = NetTest.ComplexType.GetEdfSchema();
        _rcls = new(_lst);
        _enm = new EdfTypeEnumeratorStackInlineArray();
    }

    [TestMethod]
    public void Equal_StructArray()
    {
        _lstTokenCount = 0;
        _enmToken.Reset(Shape.GetEdfSchema().Type);
        while (_enmToken.MoveNext())
        {
            _lstToken[_lstTokenCount].Token = _enmToken.CurrentToken;
            _lstToken[_lstTokenCount].Type = _enmToken.Current;
            _lstTokenCount++;
        }
        var result = _lstToken.Take(_lstTokenCount)
                              .Where(item => item.Token == Token.Value)
                              .Select(it => it.Type).ToList();
        //Assert.IsTrue(lst.SequenceEqual(result), "EdfTypeEnumeratorToken");
    }

    [TestMethod]
    public void Equal_EdfTypeEnumerators()
    {
        EdfTypeEnumeratorStack();
        List<EdfType> lst = _lst.ToList();

        EdfTypeEnumeratorStackInlineArray();
        Assert.IsTrue(lst.SequenceEqual(_lst), "EdfTypeEnumeratorYield");

        EdfTypeEnumeratorRecursive();
        Assert.IsTrue(lst.SequenceEqual(_lst), "EdfTypeEnumeratorRecursive");

        EdfTypeEnumeratorRecursiveClass();
        Assert.IsTrue(lst.SequenceEqual(_lst), "EdfTypeEnumeratorRecursiveClass");

        // максимальная глубина стэка для такой структуры - 12
        _enmToken.EnableCache = true;
        _enmToken.Reset(null);
        EdfTypeEnumeratorToken();
        var result = _lstToken.Take(_lstTokenCount)
                              .Where(item => item.Token == Token.Value)
                              .Select(it => it.Type).ToList();
        Assert.IsTrue(lst.SequenceEqual(result), "EdfTypeEnumeratorToken");

        // проверка кэша
        _lstTokenCount = 0;
        EdfTypeEnumeratorToken();
        result = _lstToken.Take(_lstTokenCount)
                      .Where(item => item.Token == Token.Value)
                      .Select(it => it.Type).ToList();
        Assert.IsTrue(lst.SequenceEqual(result), "EdfTypeEnumeratorToken cache");
    }

    public void EdfTypeEnumeratorStack()
    {
        _lst.Clear();
        foreach (var field in _schema.Type.EnumerateStack(_stack))
        {
            _lst.Add(field);
        }
    }
    public void EdfTypeEnumeratorStackInlineArray()
    {
        _lst.Clear();
        _enm.Reset(_schema.Type);
        while (_enm.MoveNext())
        {
            _lst.Add(_enm.Current);
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
    public void EdfTypeEnumeratorToken()
    {
        _lstTokenCount = 0;
        _enmToken.Reset(_schema.Type);
        while (_enmToken.MoveNext())
        {
            _lstToken[_lstTokenCount].Token = _enmToken.CurrentToken;
            _lstToken[_lstTokenCount].Type = _enmToken.Current;
            _lstTokenCount++;
        }
    }

}
