namespace EdfNet.Core;

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
    public readonly EdfTypeEnumeratorStack GetEnumerator() => new(_root, _stack);
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
public ref struct EdfTypeEnumeratorStack
{
    private readonly Span<EdfType> _stack;
    private int _sp;
    private EdfType? _current;
    // Ленивое разворачивание массива примитивов
    private uint _pendingRemaining;

    public readonly EdfType Current => _current!;

    public EdfTypeEnumeratorStack(EdfType root, Span<EdfType> stack)
    {
        _stack = stack;
        _sp = 0;
        _current = null;
        _pendingRemaining = 0;
        Push(root);
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
            if (node.Type == PoType.Struct)
            {
                if (node.Childs.Length == 0) continue;
                uint arrayCount = node.GetTotalElements();
                while (0 < arrayCount--)
                {
                    for (int c = node.Childs.Length - 1; c >= 0; c--)
                        Push(node.Childs[c]);
                }
            }
            else
            {
                uint arrayCount = node.GetTotalElements();
                _current = node;
                if (arrayCount > 1)
                    _pendingRemaining = arrayCount - 1;
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Push(EdfType node)
    {
        if (_sp >= _stack.Length)
            throw new InvalidOperationException("EDF stack overflow. Increase stackalloc EdfType[...] buffer.");
        _stack[_sp++] = node;
    }
}
