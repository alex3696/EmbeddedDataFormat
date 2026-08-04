using System.Collections;

namespace EdfNet.Core;

// ============================================================
//  Duck-typing для foreach
// ============================================================
public readonly ref struct EdfTypesYield
{
    private readonly EdfType _root;
    public EdfTypesYield(EdfType root)
    {
        _root = root;
    }
    public readonly EdfTypeEnumeratorYield GetEnumerator() => new(_root);
}
// ============================================================
//  Extension
// ============================================================
public static class EdfTypeExtensionsYield
{
    public static EdfTypesYield EnumerateYield(this EdfType root) =>
        new(root);
}
// ============================================================
//  EdfTypeEnumeratorYield
// ============================================================
public struct EdfTypeEnumeratorYield : IEnumerator<EdfType>, IEnumerator, IDisposable
{
    private readonly EdfType _edfType;
    private IEnumerator<EdfType> _enumerator;
    readonly object? IEnumerator.Current => Current;
    public readonly EdfType Current => _enumerator.Current;
    public EdfTypeEnumeratorYield(EdfType et)
    {
        _edfType = et;
        _enumerator = Process(et).GetEnumerator();
    }
    public void Dispose() { }
    void IEnumerator.Reset()
    {
        _enumerator = Process(_edfType).GetEnumerator();
    }
    public readonly bool MoveNext()
    {
        return _enumerator.MoveNext();
    }
    private static IEnumerable<EdfType> Process(EdfType et)
    {
        if (PoType.Char == et.Type)
        {
            yield return et;
        }
        uint totalElement = et.GetTotalElements();
        for (int i = 0; i < totalElement; i++)
        {
            foreach (var sub in WriteObjElement(et))
                yield return sub;
        }
    }
    private static IEnumerable<EdfType> WriteObjElement(EdfType et)
    {
        if (PoType.Struct == et.Type)
        {
            if (et.Childs != null && 0 != et.Childs.Length)
            {
                for (int childIndex = 0; childIndex < et.Childs.Length; childIndex++)
                {
                    foreach (var sub in Process(et.Childs[childIndex]))
                        yield return sub;
                }
            }
        }
        else
        {
            yield return et;
        }
    }
}
