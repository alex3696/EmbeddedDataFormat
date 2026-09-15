namespace EdfNet.Core.Text;

public enum TypeTokenType
{
    Node = 0,       // Внутренний маркер — развернуть EdfType
    Value,          // Обычное значение (примитив)
    BeginRecord,    // Начало записи (корневой элемент)
    EndRecord,      // Конец записи (корневой элемент)
    BeginStruct,    // Начало любой структуры
    EndStruct,      // Конец любой структуры
    BeginArray,     // Начало массива, (когда Dims.Length > 0 )
    EndArray,       // Конец массива
}


[DebuggerDisplay("{DebugString(),nq}")]
public struct TokenElement
{
    public TypeTokenType Token;
    public EdfType? Type;
    public void Set(TypeTokenType token, EdfType? type) { Token = token; Type = type; }
    public readonly string DebugString() => $"{Token} : {Type?.DebugString()}";
}

public struct TextEdfTypeEnumerator : IDisposable
{
    public const int MaxStackSize = 64;
    struct StackItem
    {
        public TokenElement Element;
        public uint ArrayIndex;
        public uint ArrayCount;
    }
    // Кэш для flatten-токенов
    public const int CacheSize = 1024;          // 16 KB , покрывает ~90% типов
    private readonly TokenElement[] _cache;
    private int _cacheLen;                      // >0 = cached, 0 = empty, -1 = overflow
    private EdfType? _cachedRoot;               // root, для которого построен кэш

    private readonly StackItem[] _stack;
    private int _sp;

    private TokenElement _currentElement;

    // Ленивый токен — zero-cost, не трогаем стек
    private uint _pendingCount;
    private EdfType? _pendingType;

    public readonly EdfType Current => _currentElement.Type!;
    public readonly TypeTokenType CurrentToken => _currentElement.Token;

    public TextEdfTypeEnumerator()
    {
        _stack = ArrayPool<StackItem>.Shared.Rent(MaxStackSize);
        _cache = ArrayPool<TokenElement>.Shared.Rent(CacheSize);
        Reset(null);
    }
    public readonly void Dispose()
    {
        ArrayPool<StackItem>.Shared.Return(_stack);
        ArrayPool<TokenElement>.Shared.Return(_cache);
    }

    public readonly bool IsInitialized => _cachedRoot is not null;
    // _sp используем как индекс чтения кэша, когда _cacheLen > 0
    public readonly bool IsEnded => _cacheLen > 0 ? _sp >= _cacheLen : _sp == 0;
    public bool Restart()
    {
        if (_cachedRoot is null)
            return false;
        Reset(_cachedRoot);
        return true;
    }
    // -----------------------------------------------------------------
    //  Включение/отключение кэширования
    // -----------------------------------------------------------------
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
        _pendingCount = 0;
        _pendingType = null;
        _currentElement = default;
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
            _cache[pos++] = _currentElement;
        }
        ResetState();
        return true;
    }
    private void InitStack(EdfType root)
    {
        _sp = 0;
        _cacheLen = -1;
        _pendingCount = 0;
        uint count = root.GetTotalElements();
        Push(TypeTokenType.EndRecord, root);
        PushNode(root, count);
        Push(TypeTokenType.BeginRecord, root);
    }

    public bool MoveNext()
    {
        //  линейное чтение из кэша
        if (_cacheLen > 0)
        {
            if (_sp < _cacheLen)
            {
                _currentElement = _cache[_sp++];
                return true;
            }
            return false;
        }
        // --- Fallback: оригинальная стековая логика (без изменений) ---
        if (EmitPending()) return true;
        while (_sp > 0)
        {
            if (EmitTopToken()) return true;
            if (ExpandTopNode()) return true;
        }
        return false;
    }
    // -----------------------------------------------------------------
    //  Ленивый токен
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EmitPending()
    {
        if (_pendingCount == 0) return false;

        _pendingCount--;
        _currentElement.Set(TypeTokenType.Value, _pendingType);
        return true;
    }
    // -----------------------------------------------------------------
    //  Готовый токен с верхушки стека
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EmitTopToken()
    {
        ref var top = ref _stack[_sp - 1];
        if (top.Element.Token == TypeTokenType.Node) return false;

        _currentElement = top.Element;
        _sp--;
        return true;
    }
    // -----------------------------------------------------------------
    //  Разворачивание Node. Возвращает true, если токен выдан сразу
    // -----------------------------------------------------------------
    private bool ExpandTopNode()
    {
        ref var top = ref _stack[_sp - 1];
        var node = top.Element.Type!;
        uint idx = top.ArrayIndex;
        uint count = top.ArrayCount;
        if (node.Type == EdfPrimitiveType.Char)
        {
            _sp--;
            _currentElement.Set(TypeTokenType.Value, node);
            return true;
        }
        if (node.Type == EdfPrimitiveType.Struct)
        {
            // ---- Массив / скаляр структуры ----
            if (idx < count)
            {
                // Не снимаем Node со стека — инкрементируем индекс на месте.
                // Экономим 2 операции (pop+push) на каждый элемент массива.
                top.ArrayIndex = idx + 1;
                Push(TypeTokenType.EndStruct, node);
                var childs = node.Childs;
                for (int c = childs.Length - 1; c >= 0; c--)
                {
                    var child = childs[c];
                    PushNode(child, child.GetTotalElements());
                }
                Push(TypeTokenType.BeginStruct, node);

                if (count > 1 && idx == 0)
                    Push(TypeTokenType.BeginArray, node);
                return false; // пусть MoveNext продолжит цикл
            }
            // Элементы исчерпаны — убираем Node
            _sp--;
            if (count > 1)
            {
                _currentElement.Set(TypeTokenType.EndArray, node);
                return true;
            }
            return false; // EndStruct уже был выдан ранее
        }
        else
        {
            // ---- Примитив ----
            _sp--;
            if (count > 1)
            {
                // Массив примитивов: EndArray на стек, Values — лениво
                Push(TypeTokenType.EndArray, node);
                _pendingType = node;
                _pendingCount = count;
                _currentElement.Set(TypeTokenType.BeginArray, node);
                return true;
            }
            // Скаляр — выдаём Value напрямую, не трогая стек
            _currentElement.Set(TypeTokenType.Value, node);
            return true;
        }
    }
    // -----------------------------------------------------------------
    //  Хелперы записи в inline-array — без аллокаций
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Push(TypeTokenType token, EdfType? type)
    {
        if ((uint)_sp >= MaxStackSize) ThrowOverflow();
        ref var s = ref _stack[_sp++];
        s.Element.Set(token, type);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushNode(EdfType node, uint count)
    {
        if ((uint)_sp >= MaxStackSize) ThrowOverflow();
        ref var s = ref _stack[_sp++];
        s.Element.Set(TypeTokenType.Node, node);
        s.ArrayIndex = 0;
        s.ArrayCount = count;
    }
    private static void ThrowOverflow() =>
        throw new InvalidOperationException(
            $"EDF stack overflow. Increase {nameof(MaxStackSize)}.");
}
