namespace EdfNet.Converters;

public interface IExternConverter
{
    int ToEdf(Stream src, IEdfWriter writer);
    int FromEdf(IEdfReader reader, Stream dst);
}
