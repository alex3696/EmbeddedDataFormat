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
        {
            CompositeResolver.Instance.TryRegister(GlobalResolverRegistry.BuildComposite());
            formatter = CompositeResolver.Instance.GetFormatter<T>();
        }
        if (formatter is null)
            throw new InvalidOperationException("formatter not found");
        Formatter = formatter;
    }
    public static void Register(IFormatter<T> formatter)
    {
        Formatter = formatter;
    }
}

public static class GlobalResolverRegistry
{
    static GlobalResolverRegistry()
    {
        _resolvers.Add(PrimitiveResolver.Instance);
    }

    private static readonly List<IFormatterResolver> _resolvers = new();

    // Сюда внешние проекты будут складывать свои резолверы
    public static void Register(IFormatterResolver resolver)
    {
        lock (_resolvers) { _resolvers.Add(resolver); }
    }
    public static IFormatterResolver BuildComposite()
    {
        // Собираем всё вместе, когда базовому проекту нужно выполнить сериализацию
        return CompositeResolver.Create(_resolvers.ToArray());
    }
}

public sealed class CompositeResolver : IFormatterResolver
{
    private static CompositeResolver MakeDefault()
    {
        var resolver = new CompositeResolver();
        return resolver;
    }

    public static IFormatterResolver Create(params IFormatterResolver[] resolvers)
    {
        var resolver = new CompositeResolver();
        resolver.Register(resolvers);
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
