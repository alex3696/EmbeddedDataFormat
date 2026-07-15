using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EdfNet.Interfaces;
using EdfNet.Ref;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Bench;

[EdfSerializable]
public class MyPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class ZerroAllocDecomposer
{
    [Params(1, 86_000)]
    public int Size { get; set; }

    private List<MyPosition> _list = null!;
#pragma warning disable CS8618
    AotPrimitiveDecomposer _dcs;
    MyArrayBufferWriter _buf;
#pragma warning restore CS8618

    [GlobalSetup]
    public void Setup()
    {
        _dcs = new AotPrimitiveDecomposer();
        _buf = new MyArrayBufferWriter(30000);
        _list = new(Size);
        for (int i = 0; i < Size; i++)
            _list.Add(new MyPosition() { X = i, Y = i / 2d, Z = i / 3d });
        _dcs.Decompose(new MyPosition(), _buf);
        _reflectionCache.GetOrAdd(typeof(MyPosition), t => t.GetProperties());
    }

    [Benchmark(Baseline = true)]
    public void StdReflection_GetValue()
    {
        _buf.Clear();

        foreach (var item in _list)
        {
            var props = _reflectionCache[typeof(MyPosition)];
            foreach (var prop in props)
            {
                // Тут происходит боксинг: int и double упаковываются в object, 
                // выделяя память в куче на каждом вызове!
                object? value = prop.GetValue(item);

                // Эмуляция записи (передаем в метод, который принимает object)
                WriteObjectFake(value, _buf);
                if (1000 < _buf.WrittenCount)
                    _buf.Clear();
            }
        }
    }
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _reflectionCache = new();
    private static void WriteObjectFake(object? obj, MyArrayBufferWriter writer)
    {
        if (obj is int i) PrimitiveDecomposerZeroAlloc.WriteStruct(i, writer);
        else if (obj is double d) PrimitiveDecomposerZeroAlloc.WriteStruct(d, writer);
    }

    [Benchmark]
    public void DelegateReflection_GetValue()
    {
        _buf.Clear();
        //_dcs.Decompose(_list, _buf);
        foreach (var item in _list)
        {
            _dcs.Decompose(item, _buf);
            if (1000 < _buf.WrittenCount)
                _buf.Clear();
        }
    }

    [Benchmark]
    public void Generator_GetValue()
    {
        _buf.Clear();
        //_dcs.Decompose(_list, _buf);
        foreach (var item in _list)
        {
            var enm = new MyPositionByteEnumerator(item);
            while (enm.MoveNext())
            {
                enm.Write(_buf.GetSpan(1000));
            }
            if (1000 < _buf.WrittenCount)
                _buf.Clear();
        }
    }

}
