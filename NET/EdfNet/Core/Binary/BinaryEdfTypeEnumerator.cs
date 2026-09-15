namespace EdfNet.Core.Binary;

public struct BinaryEdfTypeEnumerator : IDisposable
{
    public const int MaxStackSize = 64;
    private readonly EdfType[] _stack;

    public const int CacheSize = 1024;          // 16 KB , покрывает ~90% типов
    private readonly EdfType[] _cache;
    private int _cacheLen;                      // >0 = cached, 0 = empty, -1 = overflow
    private EdfType? _cachedRoot;               // root, для которого построен кэш

    private int _sp;
    private EdfType? _current;
    // Ленивое разворачивание массива примитивов
    private uint _pendingRemaining;
    public readonly EdfType Current => _current!;

    public BinaryEdfTypeEnumerator()
    {
        _stack = ArrayPool<EdfType>.Shared.Rent(MaxStackSize);
        _cache = ArrayPool<EdfType>.Shared.Rent(CacheSize);
        Reset(null);
    }
    public readonly void Dispose()
    {
        ArrayPool<EdfType>.Shared.Return(_stack);
        ArrayPool<EdfType>.Shared.Return(_cache);
    }
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
            if (node.Type == EdfPrimitiveType.Char)
            {
                _current = node;
                return true;
            }
            uint arrayCount = node.GetTotalElements();
            if (node.Type == EdfPrimitiveType.Struct)
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
