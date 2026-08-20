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
    public readonly bool IsEmpty => _cacheLen > 0 ? _sp >= _cacheLen : _sp == 0;

    public bool EnableCache = true;
    public void Reset(EdfType? root)
    {
        _sp = 0;
        _pendingRemaining = 0;

        if (root is null)
        {
            _cachedRoot = null;
            _cacheLen = 0;
            return;
        }

        // Если кэш отключён — гарантированно сбрасываем флаг, чтобы MoveNext не пошёл в кэш
        if (!EnableCache)
            _cacheLen = -1;

        // Cache hit — тот же root, кэш валиден и включён
        if (EnableCache && ReferenceEquals(_cachedRoot, root) && _cacheLen > 0)
            return;

        // Новый root при включённом кэше — инвалидируем старый кэш
        if (EnableCache)
            _cacheLen = 0;

        // Инициализируем стек (общая логика для cache miss и fallback)
        _stack[_sp++] = root;

        if (!EnableCache)
        {
            _cachedRoot = root;
            return;
        }

        // Пробуем построить flatten-кэш, вызывая текущий MoveNext()
        int pos = 0;
        while (MoveNext())
        {
            if (pos >= CacheSize)               // не влезает — fallback
            {
                _cacheLen = -1;
                _sp = 0;
                _pendingRemaining = 0;
                _stack[_sp++] = root;
                return;
            }
            _cache[pos++] = root;
        }
        _cachedRoot = root;
        _cacheLen = pos;                            // успешно — переключаемся на кэш
        _sp = 0;                                    // _sp теперь индекс чтения кэша
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
