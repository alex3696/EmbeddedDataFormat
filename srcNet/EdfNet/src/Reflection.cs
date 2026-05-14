using System.Collections;

namespace NetEdf.src;

public class Reflection
{
    public TypeInf GetInf(object obj)
    {
        var type = obj.GetType();
        if (IsSimpleType(type))
        {
            var inf = new TypeInf();
            inf.Type = GetPoType(type);
            return inf;

        }
        else if (obj is Array arr)
        {
            var inf = GetInf(arr.GetValue(0));
            inf.Dims = [(uint)arr.Length];
            return inf;

        }
        else
        {
            var inf = new TypeInf();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            inf.Type = PoType.Struct;
            inf.Childs = new TypeInf[props.Length];

            for(int i = 0; i < inf.Childs.Length; ++i)
            {
                var item = props[i].GetValue(obj);

                if (item != null)
                {
                    inf.Childs[i] = GetInf(item);
                    inf.Childs[i].Name = props[i].Name;
                }
            }

            return inf;
        }
        
    }
    public TypeInf GetInf(Type type)
    {
        if (IsSimpleType(type))
        {
            var inf = new TypeInf();
            inf.Type = GetPoType(type);
            return inf;
        }
        else if (type.IsArray)
        {
            var inf = GetInf(type.GetElementType());
            inf.Dims = [];
            return inf;
        }
        else
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var inf = new TypeInf();
            inf.Type = PoType.Struct;
            inf.Childs = new TypeInf[properties.Length];
            for (int i=0; i< properties.Length; ++i)
            {
                var itemType = properties[i].PropertyType;
                if (null != itemType)
                {
                    inf.Childs[i]= GetInf(itemType);
                    inf.Childs[i].Name = properties[i].Name;
                }
                    
            }
            return inf;
        }
    }




    private List<(string type, string? nameType)> GetPropertyAndName(object obj)
    {
        List<(string type, string? nameType)> result = [];

        Type type = obj.GetType();

        if (IsSimpleType(type))
        {
            result.Add((type.Name, null));
            return result;
        }
        if (obj is IEnumerable enumerable /* || type.IsArray */)
        {
            foreach (var item in enumerable)
            {
                var sub = GetPropertyAndName(item);
                result.AddRange(sub);
                return result;
            }
        }
        else
        {
            var sub = GetStructAsList(obj);
            result.AddRange(sub);
        }


        return result;
    }
    protected List<(string type, string? nameType)> GetStructAsList(object obj)
    {
        Type type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        List<(string type, string? nameType)> result = [];

        foreach (var propertyInfo in properties)
        {
            var item = propertyInfo.GetValue(obj);
            if (null != item)
                result.AddRange(GetPropertyAndName(item));
        }
        return result;
    }



    protected List<(string type, string? nameType)> GetPropertyAndName(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        List<(string type, string? nameType)> typeInfo = [];

        foreach (var propertyInfo in properties)
        {
            if (IsSimpleType1(propertyInfo))
            {
                typeInfo.Add((propertyInfo.PropertyType.Name, propertyInfo.Name));
            }
            else if (propertyInfo.PropertyType.IsArray)
            {
                typeInfo.Add((propertyInfo.PropertyType.Name, propertyInfo.Name));
            }
            else
            {
                foreach (var prop in GetPropertyAndName(propertyInfo.PropertyType))
                {
                    typeInfo.Add((propertyInfo.PropertyType.Name + " " + prop.type, propertyInfo.Name + " " + prop.nameType));
                }

            }
        }
        return typeInfo;
    }
    private bool IsSimpleType1(PropertyInfo type)
    {
        return type.PropertyType.IsPrimitive ||
               type.PropertyType.IsEnum ||
               type.PropertyType == typeof(string) ||
               type.PropertyType == typeof(decimal) ||
               type.PropertyType == typeof(DateTime) ||
               type.PropertyType == typeof(Guid);
    }
    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid);
    }

    private PoType GetPoType(Type type)
    {
        var code = TypeInfo.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Char: return PoType.Char;
            case TypeCode.SByte: return PoType.Int8;
            case TypeCode.Byte: return PoType.UInt8;
            case TypeCode.Int16: return PoType.Int16;
            case TypeCode.Int32: return PoType.Int32;
            case TypeCode.String: return PoType.String;
            case TypeCode.UInt16: return PoType.UInt16;
            case TypeCode.UInt32: return PoType.UInt32;
            case TypeCode.Int64: return PoType.Int64;
            case TypeCode.UInt64: return PoType.UInt64;
            case TypeCode.Single: return PoType.Single;
            case TypeCode.Double: return PoType.Double;
            default: return 0;
        }
    }
}










