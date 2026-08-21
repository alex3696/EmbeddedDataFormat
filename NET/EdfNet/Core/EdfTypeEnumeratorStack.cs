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
            if (node.Type == PoType.Struct)
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
// ============================================================
//  EdfTypeEnumeratorStackInlineArray
// ============================================================
public struct EdfTypeEnumeratorStackInlineArray
{
    public const int MaxStackSize = 256;
    [InlineArray(MaxStackSize)]
    private struct StackBuffer { public EdfType Slot; }
    private StackBuffer _stack;

    public const int CacheSize = 1024;          // 16 KB , покрывает ~90% типов
    [InlineArray(CacheSize)]
    private struct CacheBuffer { public EdfType Slot; }
    private CacheBuffer _cache;
    private int _cacheLen;                      // >0 = cached, 0 = empty, -1 = overflow
    private EdfType? _cachedRoot;               // root, для которого построен кэш

    private int _sp;
    private EdfType? _current;
    // Ленивое разворачивание массива примитивов
    private uint _pendingRemaining;
    public readonly EdfType Current => _current!;

    public EdfTypeEnumeratorStackInlineArray() => Reset(null);
    public readonly bool IsEnded => _cacheLen > 0 ? _sp >= _cacheLen : _sp == 0;
    public bool Restart()
    {
        if (_cachedRoot is null)
            return false;
        Reset(_cachedRoot);
        return true;
    }
    public bool EnableCache = true;
    public void Reset(EdfType? root)
    {
        ResetState();
        if (root is null)
        {
            _cachedRoot = null;
            _cacheLen = 0;
            return;
        }
        if (EnableCache)
        {
            if (ReferenceEquals(_cachedRoot, root)) // Тот же root
            {
                if (_cacheLen > 0)
                    return;                 // кэш валиден — ResetState() уже сделал rewind
                if (_cacheLen < 0)          // раньше overflow — чистый стек
                {
                    InitStack(root);        // _cacheLen уже -1, InitStack idempotent
                    return;
                }
                // _cacheLen == 0: кэш ещё не строился, падаем в построение
            }
            else
                _cacheLen = 0;              // новый root
            if (TryBuildCache(root, out int pos))//  попытка построение кэша
            {
                _cachedRoot = root;
                _cacheLen = pos;
                return;
            }
            // overflow — InitStack сам поставит _cacheLen = -1
        }
        _cachedRoot = root;
        InitStack(root);
    }
    private void ResetState()
    {
        _sp = 0;
        _pendingRemaining = 0;
        _current = null;
    }
    private bool TryBuildCache(EdfType root, out int pos)
    {
        pos = 0;
        InitStack(root);
        while (MoveNext())
        {
            if (pos >= CacheSize)
            {
                ResetState();
                return false;
            }
            _cache[pos++] = _current!;
        }
        ResetState();
        return true;
    }
    private void InitStack(EdfType root)
    {
        _sp = 0;
        _cacheLen = -1;
        _pendingRemaining = 0;
        _stack[_sp++] = root;
    }

    public bool MoveNext()
    {
        //  линейное чтение из кэша
        if (_cacheLen > 0)
        {
            if (_sp < _cacheLen)
            {
                _current = _cache[_sp++];
                return true;
            }
            return false;
        }
        // 1. Лениво выдаём оставшиеся элементы примитивного массива
        if (_pendingRemaining > 0)
        {
            _pendingRemaining--;
            return true;
        }
        while (_sp > 0)
        {
            EdfType node = _stack[--_sp];
            if (node.Type == PoType.Char)
            {
                _current = node;
                return true;
            }
            uint arrayCount = node.GetTotalElements();
            if (node.Type == PoType.Struct)
            {
                var childs = node.Childs;
                if (childs.Length == 0) continue;
                while (0 < arrayCount--)
                {
                    for (int c = childs.Length - 1; c >= 0; c--)
                    {
                        if (_sp >= MaxStackSize) ThrowOverflow();
                        _stack[_sp++] = childs[c];
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
                $"EDF stack overflow. Increase {nameof(MaxStackSize)}.");
}
