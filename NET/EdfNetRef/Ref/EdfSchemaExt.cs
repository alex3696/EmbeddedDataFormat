using EdfNet.Interfaces;
using System.Linq;
using EdfSchema = EdfNet.Core.Schema;

namespace EdfNet.Ref;

public static class EdfSchemaExt
{
    public static EdfSchema GetEdfSchemaFromType(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);

        if (!t.IsDefined(typeof(EdfSerializableAttribute), inherit: false))
            throw new InvalidOperationException($"Тип '{t.Name}' должен быть помечен атрибутом [EdfSerializable].");

        var root = BuildEdfType(t, t.Name);
        if (root is null)
            throw new InvalidOperationException($"Не удалось построить схему для типа '{t.Name}'.");

        return new EdfSchema()
        {
            Id = 0,
            Name = $"{t.Name}Schema",
            Desc = $"Schema for {t.Name} class",
            Type = root
        };
    }

    private static EdfType? BuildEdfType(Type t, string? name)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;

        if (t.IsArray)
        {
            var elementType = t.GetElementType()!;
            return BuildEdfType(elementType, name);
        }

        if (IsSimpleType(t))
        {
            return new EdfType(t.GetPoType(), name);
        }

        if (!IsStructType(t))
        {
            return null;
        }

        var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        var childs = new List<EdfType>(properties.Length);

        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;
            var propName = prop.Name;
            var propUnderlying = Nullable.GetUnderlyingType(propType) ?? propType;

            if (!IsSimpleType(propUnderlying) && !IsStructType(propUnderlying) && !propUnderlying.IsArray)
                continue;

            // --- EdfCharArrayAttribute ---
            if (HasCharArray(prop, out byte charLen))
            {
                childs.Add(new EdfType(PoType.Char, propName, [charLen]));
                continue;
            }

            // --- Размерности массива ---
            var dims = GetDims(prop);

            var child = BuildEdfType(propType, propName);
            if (child is null)
                continue;

            if (dims != null && dims.Length > 0)
            {
                if (child.Type == PoType.Struct)
                    childs.Add(new EdfType(PoType.Struct, propName, dims, child.Childs));
                else
                    childs.Add(new EdfType(child.Type, propName, dims));
            }
            else
            {
                childs.Add(child);
            }
        }

        return new EdfType(PoType.Struct, name, null, childs.ToArray());
    }

    /// <summary>
    /// Проверяет наличие <see cref="EdfCharArrayAttribute"/> и возвращает длину.
    /// Валидирует, что свойство имеет тип byte[].
    /// </summary>
    private static bool HasCharArray(PropertyInfo prop, out byte len)
    {
        var attr = prop.GetCustomAttribute<EdfCharArrayAttribute>();
        if (attr != null)
        {
            var propUnderlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (propUnderlying != typeof(byte[]))
                throw new InvalidOperationException(
                    $"EdfCharArrayAttribute может применяться только к byte[]. Свойство: {prop.Name}");
            len = attr.Len;
            return true;
        }
        len = 0;
        return false;
    }

    /// <summary>
    /// Получает размерности из <see cref="EdfArrayAttribute"/>.
    /// Валидирует, что массивы без атрибута вызывают исключение.
    /// </summary>
    private static ushort[]? GetDims(PropertyInfo prop)
    {
        var arrayAttr = prop.GetCustomAttribute<EdfArrayAttribute>();
        if (arrayAttr != null)
        {
            return arrayAttr.Dimensions;
        }
        var propUnderlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (propUnderlying.IsArray)
        {
            throw new InvalidOperationException(
                $"Массив '{prop.Name}' должен иметь атрибут [EdfArray] или [EdfCharArray].");
        }
        return null;
    }

    private static bool IsStructType(Type type)
    {
        if (type.IsClass || type.IsValueType)
        {
            if (type.IsDefined(typeof(EdfSerializableAttribute), inherit: false))
                return true;
        }
        return false;
    }
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string);
    }

    extension<T>(T)
        where T : class
    {
        public static EdfSchema GetEdfSchemaRefl() => GetEdfSchemaFromType(typeof(T));
    }
}
