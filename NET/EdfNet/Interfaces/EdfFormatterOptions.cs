namespace EdfNet.Interfaces;

public class EdfFormatterOptions
{
    public static EdfFormatterOptions Default { get; } = new EdfFormatterOptions(CompositeResolver.Instance);
    public IFormatterResolver Resolver { get; }
    public EdfFormatterOptions(IFormatterResolver resolver)
    {
        Resolver = resolver;
    }
}
