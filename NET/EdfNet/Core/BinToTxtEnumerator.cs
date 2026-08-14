namespace EdfNet.Core;

public class BlockReaderBin : BaseReaderBin
{
    public BlockReaderBin(Stream stream, Config? cfg = null)
        : base(stream, cfg)
    {
    }
}

public class ConvWriterTxt : BaseWriterTxt
{
    public ConvWriterTxt(Stream stream, Config? cfg = null)
        : base(stream, cfg)
    {
    }
}

public class RecursiveWriterBinToTxt : IPrimitiveIo
{
    #region Separators
    private void WriteSep(ReadOnlySpan<byte> src)
    {
        if (0 < Skip)
        {
            Skip--;
            return;
        }
        if (0 < src.Length)
        {
            _txtStream.Write(src);
            BytesWritted += src.Length;
        }
        PrimitivesWritted++;
    }
    public void SepRecBegin() => WriteSep(Separator.RecBegin);
    public void SepRecEnd() => WriteSep(Separator.RecEnd);
    public void SepBeginStruct() => WriteSep(Separator.BeginStruct);
    public void SepEndStruct() => WriteSep(Separator.EndStruct);
    public void SepBeginArray() => WriteSep(Separator.BeginArray);
    public void SepEndArray() => WriteSep(Separator.EndArray);
    public void SepVarEnd() => WriteSep(Separator.VarEnd);
    #endregion
    public int PrimitivesWritted { get; private set; } = 0;
    public int BytesWritted { get; private set; } = 0;
    public int Skip { get; set; } = 0;
    private readonly Stream _txtStream;


    private readonly byte[] _txtBuf = new byte[512];


    private int _blkOffset = 0;
    BlockReaderBin? _br;


    public RecursiveWriterBinToTxt(Stream txtStream)
    {
        _txtStream = txtStream;
    }
    public EdfErr DoWrite(EdfType edfType, BlockReaderBin br)
    {
        _blkOffset = 0;
        _br = br;
        do
        {
            try
            {
                EdfTypeWalker.Process(edfType, this);
            }
            catch (EdfWrongTypeException)
            {
                return EdfErr.WrongType;
            }
            catch (EdfSrcDataRequredException)
            {
                Skip = PrimitivesWritted;
                return EdfErr.SrcDataRequred;
            }
            catch (EdfDstBufOverflowException)
            {
                return EdfErr.DstBufOverflow;
            }
            PrimitivesWritted = 0;
            Skip = 0;
            if (_blkOffset < _br.GetBlockData().Length)
                continue;
            return EdfErr.IsOk;
        }
        while (true);
    }
    public void Primitive(EdfType edfType)
    {
        if (0 < Skip)
        {
            Skip--;
            return;
        }
        ArgumentNullException.ThrowIfNull(edfType, nameof(edfType));
        ArgumentNullException.ThrowIfNull(_br, nameof(_br));

        for (int i = 0; i < 2; ++i)
        {
            ReadOnlySpan<byte> srcBin = _br.GetBlockData();
            srcBin = srcBin.Slice(_blkOffset);

            ConvErr err = BinToTxt(edfType, srcBin, _txtBuf, out var readed, out var writed);
            switch (err)
            {
                case ConvErr.SrcDataRequred:
                    if (!_br.ReadBlock() || BlockType.Data != _br.GetBlockType())
                        throw new EdfSrcDataRequredException();
                    _blkOffset = 0;
                    continue;
                case ConvErr.DstBufOverflow: throw new EdfDstBufOverflowException();
                case ConvErr.WrongType: throw new EdfWrongTypeException();
                default: break;
            }
            _txtStream.Write(_txtBuf.AsSpan(0, writed));
            _blkOffset += readed;
            BytesWritted += (ushort)writed;
            PrimitivesWritted++;
            return;
        }
        throw new EdfSrcDataRequredException();
    }

    private enum ConvErr : int
    {
        SrcDataRequred = -1001,
        DstBufOverflow = -1002,
        WrongType = -1003,
        IsOk = 0,
    }

    private static ConvErr ConvertBinToTxt<T>(ReadOnlySpan<byte> srcBin, Span<byte> dstTxt, out int readed, out int writed)
        where T : struct, IUtf8SpanFormattable
    {
        writed = readed = 0;
        if (srcBin.Length < Unsafe.SizeOf<T>())
            return ConvErr.SrcDataRequred;
        readed = PrimitiveWritersBin.ReadValue(srcBin, out T? val);
        if (0 >= readed || !val.HasValue)
            return ConvErr.SrcDataRequred;
        writed = PrimitiveWritersTxt.TryWrite(dstTxt, val.Value);
        if (0 >= writed)
            return ConvErr.DstBufOverflow;
        return ConvErr.IsOk;
    }
    private static ConvErr ConvertBinToTxtString(ReadOnlySpan<byte> srcBin, Span<byte> dstTxt, out int readed, out int writed)
    {
        writed = readed = 0;
        readed = EdfBinString.ReadBin(srcBin, out var str);
        if (0 >= readed)
            return ConvErr.SrcDataRequred;
        writed = EdfBinString.WriteTxt(str, dstTxt);
        if (0 >= writed)
            return ConvErr.DstBufOverflow;
        return ConvErr.IsOk;
    }
    private static ConvErr ConvertBinToTxtChar(ReadOnlySpan<byte> srcBin, Span<byte> dst, int edfLen, out int readed, out int writed)
    {
        writed = readed = 0;
        readed = PrimitiveWritersBin.TryReadCharValue(srcBin, edfLen, out var ch);
        if (0 >= readed)
            return ConvErr.SrcDataRequred;
        writed = PrimitiveWritersTxt.TryWriteChar(dst, ch ?? [], edfLen);
        if (0 >= writed)
            return ConvErr.DstBufOverflow;
        return ConvErr.IsOk;
    }
    private static ConvErr BinToTxt(EdfType et, ReadOnlySpan<byte> srcBin, Span<byte> dstTxt, out int readed, out int writed)
    {
        switch (et.Type)
        {
            default:
            case PoType.Struct: throw new EdfWrongTypeException();
            case PoType.UInt8: return ConvertBinToTxt<byte>(srcBin, dstTxt, out readed, out writed);
            case PoType.Int8: return ConvertBinToTxt<sbyte>(srcBin, dstTxt, out readed, out writed);
            case PoType.Int16: return ConvertBinToTxt<short>(srcBin, dstTxt, out readed, out writed);
            case PoType.UInt16: return ConvertBinToTxt<ushort>(srcBin, dstTxt, out readed, out writed);
            case PoType.Int32: return ConvertBinToTxt<int>(srcBin, dstTxt, out readed, out writed);
            case PoType.UInt32: return ConvertBinToTxt<uint>(srcBin, dstTxt, out readed, out writed);
            case PoType.Int64: return ConvertBinToTxt<long>(srcBin, dstTxt, out readed, out writed);
            case PoType.UInt64: return ConvertBinToTxt<ulong>(srcBin, dstTxt, out readed, out writed);
            case PoType.Single: return ConvertBinToTxt<float>(srcBin, dstTxt, out readed, out writed);
            case PoType.Double: return ConvertBinToTxt<double>(srcBin, dstTxt, out readed, out writed);
            case PoType.String: return ConvertBinToTxtString(srcBin, dstTxt, out readed, out writed);
            case PoType.Char: return ConvertBinToTxtChar(srcBin, dstTxt, (int)et.GetTotalElements(), out readed, out writed);
        }
    }
}
