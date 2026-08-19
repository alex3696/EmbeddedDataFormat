namespace EdfNet.Core;

public enum Token
{
    Value = 0,      // Обычное значение (примитив)
    BeginRecord,    // Начало записи (корневой элемент)
    EndRecord,      // Конец записи (корневой элемент)
    BeginStruct,    // Начало любой структуры
    EndStruct,      // Конец любой структуры
    BeginArray,     // Начало массива, (когда Dims.Length > 0 )
    EndArray,       // Конец массива
    Node,           // Внутренний маркер — развернуть EdfType
}

struct StackItem
{
    public Token Token;
    public EdfType? Type;
    public uint ArrayIndex;
    public uint ArrayCount;
}

public struct EdfTypeEnumeratorToken
{
    public const int MaxStackSize = 64;

    [InlineArray(MaxStackSize)]
    private struct StackBuffer { public StackItem Slot; }

    private StackBuffer _stack;
    private int _sp;

    private EdfType? _current;
    private Token _currentToken;

    // Ленивый токен — zero-cost, не трогаем стек
    private uint _pendingCount;
    private EdfType? _pendingType;

    public readonly EdfType Current => _current!;
    public readonly Token CurrentToken => _currentToken;

    public EdfTypeEnumeratorToken(EdfType? root) => Reset(root);
    public readonly bool IsEmpty => _sp == 0;

    public void Reset(EdfType? root)
    {
        _sp = 0;
        _current = null;
        _currentToken = Token.Value;
        _pendingCount = 0;

        if (root is null) return;

        uint count = root.GetTotalElements();

        // LIFO: EndRecord на дне, BeginRecord наверху
        Push(Token.EndRecord, root);
        PushNode(root, count);
        Push(Token.BeginRecord, root);
    }

    // -----------------------------------------------------------------
    //  Оркестратор — не инлайним во внешний код, т.к. метод большой
    // -----------------------------------------------------------------
    public bool MoveNext()
    {
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
