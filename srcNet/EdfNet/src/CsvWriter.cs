using System;
using System.Collections.Generic;
using System.Text;

namespace NetEdf.src;

public class CsvWriter : TxtWriter
{
    readonly Stream _st;
    public CsvWriter(Stream stream, Header? cfg = null) : base(stream, cfg)
    {
        _st = stream;
        SepBeginStruct = null;
        SepEndStruct = null;
        SepBeginArray = null;
        SepEndArray = null;
        SepVarEnd = ";"u8.ToArray();
        SepRecBegin = null;
        SepRecEnd = null;
        if (0 == stream.Position)
            Write(Cfg);
    }
    public override void Write(Header h)
    {
        Flush();
        Write($"version;blocksize;encoding;flags;\n" +
            $"{h.VersMajor}.{h.VersMinor};{h.Blocksize};{h.Encoding};{(uint)h.Flags};\n\n");
        _currDataType = null;
        _blkQty = 0;

    }
    public override void Write(TypeRec t)
    {
        Flush();

        ToString(t.Inf);

        _currDataType = t.Inf;
        _blkQty = 0;
        Write("\n");
    }
    protected void ToString(TypeInf t)
    {
        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            foreach (var it in t.Childs)
            {
                ToString(it);
            }
        }
        else
        {
            if (null != t.Dims)
            {
                if (t.Dims.Length == 0)
                {
                    if (!string.IsNullOrEmpty(t.Name))
                        Write($"{t.Name};");
                }
                else if (t.Dims.Length > 1)
                {
                    int countRowDims = (int)t.Dims[0];
                    int countColumnDims = (int)t.Dims[1];

                    for (int i = 0; i < countRowDims; ++i)
                    {
                        for (int j = 0; j < countColumnDims; j++)
                        {
                            Write($"{t.Name}[{i},{j}];");
                        }
                    }
                }
                else
                {
                    foreach (var d in t.Dims)
                    {
                        for (int i = 0; i < d; ++i)
                        {
                            Write($"{t.Name}[{i}];");
                        }
                    }
                }

            }
        }
    }
}
