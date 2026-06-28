using NetEdf.Base;
using System.Text.RegularExpressions;

namespace NetEdf.src;

public class TextReader : BaseDisposable
{
    public readonly Header Cfg; // Заголовок
    readonly StreamReader _tr; // Поток для чтения данных
    protected TypeInf? _cureDataType;

    public TextReader(Stream stream, Header? cfg = null) // конструктор, принимает поток для записи и необязательную конфигурацию
    {
        _tr = new StreamReader(stream);
        Cfg = Header.Default;
        var newCfg = ReadHeader();
    }

    public string ReadNextLine()
    {
        string line = null;
        while ((line = _tr.ReadLine()) != null)
        {
            line = line.Trim();
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
                return line;
        }
        return null;
    }

    public Header? ReadHeader()
    {
        string? str = ReadNextLine();
        var headerRegex = new Regex(@"(\w+)\s*=\s*([^;]+)");

        if (headerRegex.IsMatch(str))
        {
            Header header = new();

            EdfErr err;
            string cleanHeader = Regex.Replace(str, @"\D+", " ").Trim();
            string[] arr = cleanHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            Span<byte> element = Encoding.UTF8.GetBytes(arr[0]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt8, element, out object versMajor)))
                return null;
            header.VersMajor = (byte)versMajor;

            element = Encoding.UTF8.GetBytes(arr[1]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt8, element, out object versMinor)))
                return null;
            header.VersMinor = (byte)versMinor;

            element = Encoding.UTF8.GetBytes(arr[2]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt16, element, out object blocksize)))
                return null;
            header.Blocksize = (ushort)blocksize;

            element = Encoding.UTF8.GetBytes(arr[3]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt16, element, out object encoding)))
                return null;
            header.Encoding = (ushort)encoding;

            element = Encoding.UTF8.GetBytes(arr[4]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt32, element, out object flags)))
                return null;
            header.Flags = (Options)(uint)flags;

            return header;
        }
        return null;
    }

    public TypeRec ReadInfo()
    {
        string? str = ReadNextLine();
        var typeRecRegex = new Regex(@"^<\?\s*\{\s*(?<id>\d+)\s*;\s*""(?<name>[^""]*)""\s*\}\s*(?<tail>.*)");
        var match = typeRecRegex.Match(str);

        if (match.Success)
        {
            string[] arr = [
                match.Groups["id"].Value,
                match.Groups["name"].Value,
                match.Groups["tail"].Value
                ];
            EdfErr err;
            TypeRec rec = new();

            Span<byte> element = Encoding.UTF8.GetBytes(arr[0]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt32, element, out object recId)))
                return null;
            rec.Id = (uint)recId;

            element = Encoding.UTF8.GetBytes(arr[1]);
            if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.String, element, out object recName)))
                return null;
            rec.Name = (string)recName;

            rec.Desc = null;

            string fullSchema = str;
            int openBraces = CountChars(fullSchema, '{');
            int closeBraces = CountChars(fullSchema, '}');
            while (openBraces > closeBraces || !fullSchema.Trim().EndsWith(">"))
            {
                string? nextLine = _tr.ReadLine();
                if (nextLine == null) break;

                fullSchema += " " + nextLine.Trim();
                openBraces = CountChars(fullSchema, '{');
                closeBraces = CountChars(fullSchema, '}');
            }
            rec.Inf = ParseInf(fullSchema);
            _cureDataType = rec.Inf;

            return rec;
        }
        return null;
    }

    private int CountChars(string str, char sym)
    {
        int count = 0;
        foreach (var s in str)
        {
            if (s == sym)
                count++;
        }
        return count;
    }
    private TypeInf ParseInf(string str)
    {
        str = str.Trim();

        if (str.StartsWith("<?") && str.EndsWith(">"))
        {
            int headerEnd = str.IndexOf('}');
            if (headerEnd != -1)
            {
                str = str.Substring(headerEnd + 1, str.Length - (headerEnd + 1) - 1).Trim();
            }
        }
        return ParseNextType(ref str);
    }

    private TypeInf ParseNextType(ref string str)
    {
        EdfErr err;
        str = str.Replace("\"", "").Replace("\\", "").Trim();
        int index = 0;

        while (char.IsLetterOrDigit(str[index]))
            index++;
        string type = str.Substring(0, index);
        if (!Enum.TryParse(type, true, out PoType potype))
            return null;

        str = str.Substring(index).TrimStart().Trim();

        List<uint>? dimsSize = new();
        while (str.StartsWith('['))
        {
            int closeBacket = str.IndexOf(']');
            string dimsCount = str.Substring(1, closeBacket - 1);
            var parts = dimsCount.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.UInt32, Encoding.UTF8.GetBytes(part), out object dims)))
                    return null;
                dimsSize.Add((uint)dims);
                str = str.Substring(closeBacket + 1).TrimStart();
            }
        }
        index = 0;
        while (char.IsLetterOrDigit(str[index]))
            index++;
        string name = str.Substring(0, index);
        str = str.Substring(index).TrimStart().Trim();
        if (EdfErr.IsOk != (err = Primitives.TryTxtToSrc(PoType.String, Encoding.UTF8.GetBytes(name), out object nameType)))
            return null;

        List<TypeInf>? childs = new();
        if (potype == PoType.Struct)
        {
            str = str.Substring(1).TrimStart();
            while (str != null)
            {
                if (str.StartsWith('}'))
                {
                    str = str.Substring(1).TrimStart();
                    break;
                }
                childs.Add(ParseNextType(ref str));

                if (str.StartsWith(';'))
                    str = str.Substring(1).TrimStart();
            }
        }
        return new TypeInf(potype, nameType.ToString(), dimsSize.ToArray(), childs.ToArray());
    }

    private string[] ExtractData(string line)
    {
        var matches = Regex.Matches(line, @"(""(?:[^""\\]|\\.)*""|[^\{\}\[\]\s;,]+)");
        return matches.Cast<Match>()
            .Select(m => m.Value).Where(v => v != "<=" && v != ">").ToArray();
    }

    object? _ret; 

    public EdfErr TryRead<T>(out T? ret)
    {
        ArgumentNullException.ThrowIfNull(_cureDataType);
        EdfErr err;
        ret = default;

        string line = ReadNextLine();

        string[] str = ExtractData(line);
        int index = 0;

        err = ReadObject(_cureDataType, str, typeof(T), ref index, out _ret);

        switch (err)
        {
            case EdfErr.IsOk:
                ret = (T?)_ret;
                return err;
            case EdfErr.SrcDataRequred:
                return err;
            case EdfErr.WrongType:
                return err;
            case EdfErr.DstBufOverflow:
                return err;
        }
        return err;

    }

    private EdfErr ReadObject(TypeInf t, string[] str, Type csType, ref int index, out object? ret)
    {
        uint totalElements = t.GetTotalElements();
        if (totalElements > 1)
            return ReadArray(t, str, ref index, csType, totalElements, out ret);
        return ReadElement(t, str, ref index, csType, out ret);
    }
    private EdfErr ReadElement(TypeInf t, string[] str, ref int index, Type csType, out object? ret)
    {
        if (t.Type == PoType.Struct)
            return ReadStruct(t, str, ref index, csType,out ret);
        return ReadPrimitive(t, str, ref index, csType, out ret);
    }
    private EdfErr ReadArray(TypeInf t, string[] str, ref int index, Type csType, uint totalElement, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        if(!csType.IsArray)
            throw new ArrayTypeMismatchException();

        var elementType = csType.GetElementType(); 
        ArgumentNullException.ThrowIfNull(elementType);

        var arr = Array.CreateInstance(elementType, totalElement);
        ret = arr;

        for(int i = 0; i < totalElement; i++)
        {
            if (EdfErr.IsOk != (err = ReadElement(t, str, ref index, elementType, out var arrItem)))
                return err;
            arr.SetValue(arrItem, i);
        }
        return err;

    }
    private EdfErr ReadStruct(TypeInf t, string[] str, ref int index, Type csType, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        ret = default;

        Type actualType = Nullable.GetUnderlyingType(csType) ?? csType;

        if(t.Childs == null || t.Childs.Length == 0)
            return EdfErr.IsOk;

        ret = Activator.CreateInstance(actualType);

        var fields = actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        int fieldId = 0;

        foreach(var child in t.Childs)
        {
            var field = fields[fieldId++];
            if (EdfErr.IsOk != (err = ReadObject(child, str, field.PropertyType, ref index, out var childVal)))
                return err;

            field.SetValue(ret, childVal);
        }
        return err;
    }
    private EdfErr ReadPrimitive(TypeInf t, string[] str, ref int index, Type csType, out object? ret)
    {
        ret = default;
        EdfErr err = EdfErr.IsOk;
        var value = str[index++];

        if((err = Primitives.TryTxtToSrc(t.Type, Encoding.UTF8.GetBytes(value), out ret)) != 0)
            return err;
        return err;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;
    }
}
