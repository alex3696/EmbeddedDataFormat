namespace EdfNet.Core.Binary;

// ============================================================
//  Duck-typing для foreach
// ============================================================
public readonly ref struct EdfTypesStack
{
    private readonly EdfType _root;
    private readonly Span<EdfType> _stack;
    public EdfTypesStack(EdfType root, Span<EdfType> stack)
    {
        _root = root;
        _stack = stack;
    }
    public readonly BinaryEdfTypeEnumeratorStack GetEnumerator() => new(_root, _stack);
}
// ============================================================
//  Extension
// ============================================================
public static class EdfTypeExtensionsStack
{
    public static EdfTypesStack EnumerateStack(this EdfType root, Span<EdfType> stack) =>
        new(root, stack);
}
// ============================================================
//  EdfTypeEnumeratorStack
// ============================================================
public ref struct BinaryEdfTypeEnumeratorStack
{
    private readonly Span<EdfType> _stack;
    private int _sp;
    private EdfType? _current;
    // Ленивое разворачивание массива примитивов
    private uint _pendingRemaining;

    public readonly EdfType Current => _current!;
    public BinaryEdfTypeEnumeratorStack(EdfType root, Span<EdfType> stack)
    {
        _stack = stack;
        _sp = 0;
        _current = null;
        _pendingRemaining = 0;
        _stack[_sp++] = root;
    }
    public bool MoveNext()
    {
        // 1. Лениво выдаём оставшиеся элементы примитивного массива
        if (_pendingRemaining > 0)
        {
            _pendingRemaining--;
            return true;
        }
        while (_sp > 0)
        {
            var node = _stack[--_sp];
            uint arrayCount = node.GetTotalElements();
            if (node.Type == EdfPrimitiveType.Struct)
            {
                if (node.Childs.Length == 0) continue;
                while (0 < arrayCount--)
                {
                    for (int c = node.Childs.Length - 1; c >= 0; c--)
                    {
                        if (_sp >= _stack.Length) ThrowOverflow();
                        _stack[_sp++] = node.Childs[c];
                    }
                }
            }
            else
            {
                _current = node;
                if (arrayCount > 1)
                    _pendingRemaining = arrayCount - 1;
                return true;
            }
        }
        return false;
    }
    private static void ThrowOverflow() =>
        throw new InvalidOperationException(
            $"EDF stack overflow. Increase stackalloc EdfType[...] buffer.");
}
