namespace EdfNet.Interfaces;

public static class EmptyObjectCache<T> where T : new()
{
    // Экземпляр создается один раз при первом обращении к типу T
    public static T Instance = new();
}

public static class EdfProvider<T>
{
    // Поле инициализируется один раз для каждого типа T
    public static IFormatter<T> Formatter;
    static EdfProvider()
    {
        var formatter = CompositeResolver.Instance.GetFormatter<T>();
        if (formatter is null)
            throw new InvalidOperationException("formatter not found");
        Formatter = formatter;
    }
    public static void Register(IFormatter<T> formatter)
    {
        Formatter = formatter;
    }
}

public sealed class CompositeResolver : IFormatterResolver
{
    private static CompositeResolver MakeDefault()
    {
        var resolver = new CompositeResolver();
        return resolver;
    }

    public static readonly CompositeResolver Instance = MakeDefault();
    public bool IsRegistred => _isRegistred;

    private IFormatterResolver[] _resolvers = [];
    private bool _isRegistred;


    private CompositeResolver() { }

    public bool TryRegister(params IFormatterResolver[] resolvers)
    {
        if (_isRegistred) return false;
        _resolvers = resolvers;
        _isRegistred = true;
        return true;
    }
    public void Register(params IFormatterResolver[] resolvers)
    {
        if (!TryRegister(resolvers))
            throw new InvalidOperationException("already registred");
    }

    public IFormatter<T>? GetFormatter<T>()
    {
        for (int i = 0; i < _resolvers.Length; i++)
        {
            var formatter = _resolvers[i].GetFormatter<T>();
            if (null != formatter)
                return formatter;
        }
        return null;
    }

}
