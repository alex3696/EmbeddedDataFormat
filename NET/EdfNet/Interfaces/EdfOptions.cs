namespace EdfNet.Interfaces;

public class EdfOptions
{
    public static EdfOptions Default { get; } = new EdfOptions(CompositeResolver.Instance);
    public IFormatterResolver Resolver { get; }
    public EdfOptions(IFormatterResolver resolver)
    {
        Resolver = resolver;
    }
}
