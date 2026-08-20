namespace EdfNet.Core;

public enum Token
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
public readonly struct TokenElement
{
    public readonly Token Token;
    public readonly EdfType? Type;
    public TokenElement(Token token, EdfType? type) { Token = token; Type = type; }
    public string DebugString() => $"{Token} : {Type?.DebugString()}";
}

public struct EdfTypeEnumeratorToken
{
    public const int MaxStackSize = 64;
    struct StackItem
    {
        public Token Token;
        public EdfType? Type;
        public uint ArrayIndex;
        public uint ArrayCount;
    }
    [InlineArray(MaxStackSize)]
    private struct StackBuffer { public StackItem Slot; }

    // Кэш для flatten-токенов
    public const int CacheSize = 1024;          // 16 KB , покрывает ~90% типов
    [InlineArray(CacheSize)]
    private struct CacheBuffer { public TokenElement Slot; }
    private CacheBuffer _cache;
    private int _cacheLen;                      // >0 = cached, 0 = empty, -1 = overflow
    private EdfType? _cachedRoot;               // root, для которого построен кэш

    private StackBuffer _stack;
    private int _sp;

    private EdfType? _current;
    private Token _currentToken;

    // Ленивый токен — zero-cost, не трогаем стек
    private uint _pendingCount;
    private EdfType? _pendingType;

    public readonly EdfType Current => _current!;
    public readonly Token CurrentToken => _currentToken;

    public EdfTypeEnumeratorToken() => Reset(null);

    // MODIFIED: _sp используем как индекс чтения кэша, когда _cacheLen > 0
    public readonly bool IsEmpty => _cacheLen > 0 ? _sp >= _cacheLen : _sp == 0;
    // -----------------------------------------------------------------
    //  Включение/отключение кэширования
    // -----------------------------------------------------------------
    public bool EnableCache = true;

    public void Reset(EdfType? root)
    {
        _sp = 0;
        _pendingCount = 0;
        _current = null;
        _currentToken = Token.Node;

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
        InitStack(root);

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
                _pendingCount = 0;
                InitStack(root);
                return;
            }
            _cache[pos++] = new TokenElement(_currentToken, _current);
        }

        _cachedRoot = root;
        _cacheLen = pos;                            // успешно — переключаемся на кэш
        _sp = 0;                                    // _sp теперь индекс чтения кэша
    }
    private void InitStack(EdfType root)
    {
        uint count = root.GetTotalElements();

        // LIFO: EndRecord на дне, BeginRecord наверху
        Push(Token.EndRecord, root);
        PushNode(root, count);
        Push(Token.BeginRecord, root);
    }

    public bool MoveNext()
    {
        //  линейное чтение из кэша
        if (_cacheLen > 0)
        {
            if (_sp < _cacheLen)
            {
                ref var item = ref _cache[_sp++];
                _current = item.Type;
                _currentToken = item.Token;
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
        _currentToken = Token.Value;
        _current = _pendingType;
        return true;
    }
    // -----------------------------------------------------------------
    //  Готовый токен с верхушки стека
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EmitTopToken()
    {
        ref var top = ref _stack[_sp - 1];
        if (top.Token == Token.Node) return false;

        _current = top.Type;
        _currentToken = top.Token;
        _sp--;
        return true;
    }
    // -----------------------------------------------------------------
    //  Разворачивание Node. Возвращает true, если токен выдан сразу
    // -----------------------------------------------------------------
    private bool ExpandTopNode()
    {
        ref var top = ref _stack[_sp - 1];
        var node = top.Type!;
        uint idx = top.ArrayIndex;
        uint count = top.ArrayCount;
        if (node.Type == PoType.Char)
        {
            _sp--;
            _current = node;
            _currentToken = Token.Value;
            return true;
        }
        if (node.Type == PoType.Struct)
        {
            // ---- Массив / скаляр структуры ----
            if (idx < count)
            {
                // Не снимаем Node со стека — инкрементируем индекс на месте.
                // Экономим 2 операции (pop+push) на каждый элемент массива.
                top.ArrayIndex = idx + 1;
                Push(Token.EndStruct, node);
                var childs = node.Childs;
                for (int c = childs.Length - 1; c >= 0; c--)
                {
                    var child = childs[c];
                    PushNode(child, child.GetTotalElements());
                }
                Push(Token.BeginStruct, node);

                if (count > 1 && idx == 0)
                    Push(Token.BeginArray, node);
                return false; // пусть MoveNext продолжит цикл
            }
            // Элементы исчерпаны — убираем Node
            _sp--;
            if (count > 1)
            {
                _current = node;
                _currentToken = Token.EndArray;
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
                Push(Token.EndArray, node);
                _pendingType = node;
                _pendingCount = count;
                _current = node;
                _currentToken = Token.BeginArray;
                return true;
            }
            // Скаляр — выдаём Value напрямую, не трогая стек
            _current = node;
            _currentToken = Token.Value;
            return true;
        }
    }
    // -----------------------------------------------------------------
    //  Хелперы записи в inline-array — без аллокаций
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Push(Token token, EdfType? type)
    {
        if ((uint)_sp >= MaxStackSize) ThrowOverflow();
        ref var s = ref _stack[_sp++];
        s.Token = token;
        s.Type = type;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushNode(EdfType node, uint count)
    {
        if ((uint)_sp >= MaxStackSize) ThrowOverflow();
        ref var s = ref _stack[_sp++];
        s.Token = Token.Node;
        s.Type = node;
        s.ArrayIndex = 0;
        s.ArrayCount = count;
    }
    private static void ThrowOverflow() =>
        throw new InvalidOperationException(
            $"EDF stack overflow. Increase {nameof(MaxStackSize)}.");
}
