using System.Collections.Concurrent;

namespace EdfNet.Interfaces;
/// <summary>
/// Реестр схем
/// </summary>
public static class EdfSchemaRegistry
{
    private static readonly ConcurrentDictionary<Type, Lazy<EdfSchema>> _schemas = new();
    public static void Register<T>(Func<EdfSchema> factory) => _schemas[typeof(T)] = new Lazy<EdfSchema>(factory);
    public static EdfSchema? Get<T>() => _schemas.TryGetValue(typeof(T), out var val) ? val.Value : null;
}
/// <summary>
/// Снимок схемы на момент первого обращения. Перерегистрация после первого Get не влияет на этот кэш.
/// </summary>
public static class ObjectSchemaCache<T>
{
    private static readonly Lazy<EdfSchema?> _lazy = new(() => EdfSchemaRegistry.Get<T>());
    public static EdfSchema? Instance => _lazy.Value;
}
public static class IEdfWriter_WriteInfData_SchemaRegistry_Extension
{
    public static EdfErrorCode WriteInfData<T>(this IEdfWriter writer
        , ushort schId, string? schName, string? schDesc, T value)
    {
        var schema = ObjectSchemaCache<T>.Instance
            ?? throw new EdfSchemaNotRegisteredException(typeof(T));
        if (schId != default) schema.Id = schId;
        if (schName != default) schema.Name = schName;
        if (schema.Id != default) schema.Desc = schDesc;
        writer.WriteSchema(schema);
        return writer.WriteValue(value);
    }
    public static EdfErrorCode WriteInfData<T>(this IEdfWriter writer, T value)
        => WriteInfData(writer, default, default, default, value);
}
