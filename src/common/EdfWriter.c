#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int EdfWriteBlockBin(Stream_t* st, const EdfHeader_t* cfg, const EdfBlock_t* blk, size_t* writed)
{
	UNUSED(cfg);
	int err = 0;
	uint16_t* blkCrc = (uint16_t*)((uint8_t*)blk + 4 + blk->Len);
	*blkCrc = MbCrc16(blk, 4 + blk->Len);
	if ((err = StreamWrite(st, NULL, blk, 4 + blk->Len + 2)))
		return err;
	*writed = blk->Len;
	return 0;
}
//-----------------------------------------------------------------------------

// Write Header
//-----------------------------------------------------------------------------
int EdfWriteHeader(EdfWriter_t* dw, const EdfHeader_t* h, size_t* writed)
{
	if (!dw->WriteHeader || !h)
		return ERR_FN_NOT_EXIST;
	dw->BlkSeq = 0;
	dw->h = *h;
	int err = (*dw->WriteHeader)(dw, h, writed);
	if (err)
	{
		LOG_ERR();
		return err;
	}
	dw->BlkSeq++;
	dw->DatLen = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int EdfWriteHeaderBin(EdfWriter_t* dw, const EdfHeader_t* h, size_t* writed)
{
	dw->BlkType = (uint8_t)btHeader;
	dw->DatLen = (uint16_t)HeaderToBytes(h, dw->Block);
	return EdfWriteBlockBin(&dw->Stream, h, (EdfBlock_t*)&dw->BlkType, writed);
}
//-----------------------------------------------------------------------------
static int EdfWriteHeaderTxt(EdfWriter_t* dw, const EdfHeader_t* h, size_t* writed)
{
	return StreamWriteFmt(&dw->Stream, writed, "<~ {version=%d.%d; bs=%d; encoding=%d; flags=%d; } >\n"
		, h->VersMajor, h->VersMinor
		, h->Blocksize, h->Encoding, h->Flags);
}

// Write Info
//-----------------------------------------------------------------------------
int EdfWriteInfo(EdfWriter_t* dw, const TypeRec_t* t, size_t* writed)
{
	int err = 0;
	size_t flushed = 0;
	if ((err = EdfFlushDataBlock(dw, &flushed)))
		return err;
	dw->Skip = 0;

	if (!dw->WriteInfo || !t)
		return ERR_FN_NOT_EXIST;
	err = (*dw->WriteInfo)(dw, t, writed);
	if (err)
	{
		LOG_ERR();
		return err;
	}
	dw->t = t;
	//dw->TypeFlag |= HasDynamicFields(&t->Inf);
	//dw->TypeLen = GetTypeCSize(&t->Inf);
	dw->BlkSeq++;
	dw->DatLen = 0;
	dw->BufLen = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int EdfWriteInfoBin(EdfWriter_t* dw, const TypeRec_t* t, size_t* writed)
{
	int err = 0;
	dw->BlkType = (uint8_t)btVarInfo;
	MemStream_t ms = { 0 };
	size_t w = 0;
	if ((err = MemStreamOutOpen(&ms, dw->Block, sizeof(dw->Block))) ||
		(err = StreamWriteInfBin((Stream_t*)&ms, t, &w)))
		return err;
	dw->DatLen = (uint16_t)w;// (uint16_t)ms.WPos;
	if ((err = EdfWriteBlockBin(&dw->Stream, &dw->h, (EdfBlock_t*)&dw->BlkType, writed)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
static int EdfWriteInfoTxt(EdfWriter_t* w, const TypeRec_t* t, size_t* writed)
{
	return StreamWriteInfTxt(&w->Stream, t, writed);
}

// Write Data
//-----------------------------------------------------------------------------
int EdfFlushDataBlock(EdfWriter_t* dw, size_t* writed)
{
	if (NULL == dw->FlushData || 0 == dw->DatLen)
		return 0;
	int err = (*dw->FlushData)(dw, writed);
	if (err)
	{
		LOG_ERR();
		return err;
	}
	dw->BlkSeq++;
	dw->DatLen = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataBin(EdfWriter_t* dw, size_t* writed)
{
	dw->BlkType = (uint8_t)btVarData;
	return EdfWriteBlockBin(&dw->Stream, &dw->h, (EdfBlock_t*)&dw->BlkType, writed);
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataTxt(EdfWriter_t* dw, size_t* writed)
{
	return StreamWrite((Stream_t*)&dw->Stream, writed, dw->Block, dw->DatLen);
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static int SeekEnd(EdfWriter_t* f)
{
	int err = 0;
	while (!(err = EdfReadBlock(f)))
	{
		switch (f->BlkType)
		{
		default: break;
		case btHeader:
			if (HEADER_SIZE == f->DatLen)
			{
				err = MakeHeaderFromBytes(f->Block, f->DatLen, &f->h);
			}
			break;
		case btVarInfo:
		{
		}
		break;
		case btVarData:
		{
		}
		break;
		}//switch
		if (0 != err)
		{
			LOG_ERR();
			break;
		}
	}//while
	if (ERR_EOF == err)
		err = 0;
	return err;
}
//-----------------------------------------------------------------------------
int EdfOpenStream(EdfWriter_t* f, Stream_t* stream, const char* mode)
{
	if (2 > strnlength(mode, 2))
		return ERR_WRONG_PARAMETERS;
	int err = 0;
	f->t = NULL;
	memset(&f->h, 0, sizeof(EdfHeader_t));
	if (0 == strncmp("wb", mode, 2) || 0 == strncmp("ab", mode, 2))
	{
		f->Stream = *stream;
		f->BlkSeq = 0;
		f->Skip = 0;
		f->DatLen = 0;
		f->BufLen = 0;
		f->WritePrimitive = strchr(mode, 'c') ? BinToBin : CBinToBin;
		f->WriteHeader = EdfWriteHeaderBin;
		f->WriteInfo = EdfWriteInfoBin;
		f->FlushData = StreamWriteBlockDataBin;
		f->BeginStruct = NULL;
		f->EndStruct = NULL;
		f->BeginArray = NULL;
		f->EndArray = NULL;
		f->SepVarEnd = NULL;
		f->RecBegin = NULL;
		f->RecEnd = NULL;
		if (strchr(mode, 'a'))
		{
			err = SeekEnd(f);
		}
	}
	else if (0 == strncmp("wt", mode, 2) || 0 == strncmp("at", mode, 2))
	{
		f->Stream = *stream;
		f->BlkSeq = 0;
		f->Skip = 0;
		f->DatLen = 0;
		f->BufLen = 0;
		f->WritePrimitive = strchr(mode, 'c') ? BinToStr : CBinToStr;
		f->WriteHeader = EdfWriteHeaderTxt;
		f->WriteInfo = EdfWriteInfoTxt;
		f->FlushData = StreamWriteBlockDataTxt;
		f->BeginStruct = SepBeginStruct;
		f->EndStruct = SepEndStruct;
		f->BeginArray = SepBeginArray;
		f->EndArray = SepEndArray;
		f->SepVarEnd = SepVarEnd;
		f->RecBegin = SepRecBegin;
		f->RecEnd = SepRecEnd;
		if (strchr(mode, 'a'))
		{
			err = StreamSeek(stream, 0, FSEEK_END);
		}
		return err;
	}
	else if (0 == strncmp("rb", mode, 2))
	{
		f->Stream = *stream;
		f->BlkSeq = 0;
		f->Skip = 0;
		f->DatLen = 0;
		f->BufLen = 0;
		f->WritePrimitive = BinToBin;
		f->WriteHeader = NULL;
		f->WriteInfo = NULL;
		f->FlushData = NULL;
		f->BeginStruct = NULL;
		f->EndStruct = NULL;
		f->BeginArray = NULL;
		f->EndArray = NULL;
		f->SepVarEnd = NULL;
		f->RecBegin = NULL;
		f->RecEnd = NULL;
	}
	if (0 == strncmp("rt", mode, 2))
	{
		err = ERR_WRONG_PARAMETERS;
	}
	return err;
}
//-----------------------------------------------------------------------------
int EdfOpen(EdfWriter_t* edf, const char* file, const char* mode)
{
	return EdfOpenWithFs(edf, file, mode, FileStreamOpen);
}
//-----------------------------------------------------------------------------
int EdfOpenWithFs(EdfWriter_t* edf, const char* file, const char* mode, FileStreamOpenFn fnOpen)
{
	if (2 > strnlength(mode, 2))
		return ERR_WRONG_PARAMETERS;
	int err = 0;
	if (0 == strncmp("wb", mode, 2) || 0 == strncmp("ab", mode, 2))
	{
		if ((err = (*fnOpen)((FileStream_t*)&edf->Stream, file, mode)))
			return err;
		return EdfOpenStream(edf, &edf->Stream, mode);
	}
	else if (0 == strncmp("wt", mode, 2) || 0 == strncmp("at", mode, 2))
	{
		char* filemode;
		if (0 == strncmp("wt", mode, 2))
			filemode = "wb";
		else if (0 == strncmp("at", mode, 2))
			filemode = "ab";
		else
			return ERR_WRONG_PARAMETERS;
		if ((err = (*fnOpen)((FileStream_t*)&edf->Stream, file, filemode)))
			return err;
		return EdfOpenStream(edf, &edf->Stream, mode);
	}
	else if (0 == strncmp("rb", mode, 2))
	{
		if ((err = (*fnOpen)((FileStream_t*)&edf->Stream, file, "rb")))
			return err;
		return EdfOpenStream(edf, &edf->Stream, mode);
	}
	else if (0 == strncmp("rt", mode, 2))
	{
		return ERR_WRONG_PARAMETERS;
	}
	return ERR_WRONG_PARAMETERS;
}
//-----------------------------------------------------------------------------
int EdfClose(EdfWriter_t* dw)
{
	int err = 0;
	size_t w = 0;
	if ((err = EdfFlushDataBlock(dw, &w)))
		return err;
	return StreamClose(&dw->Stream);
}
//-----------------------------------------------------------------------------
int EdfWriteSep(const char* const src,
	uint8_t** dst, size_t* dstSize,
	size_t* skip, size_t* wqty,
	size_t* writed)
{
	if (0 < (*skip))
	{
		(*skip)--;
		return 0;
	}
	size_t srcLen = src ? strnlength(src, 10) : 0;
	if (!srcLen)
	{
		(*wqty)++;
		return 0;
	}
	if (srcLen > *dstSize)
		return ERR_DST_SHORT;
	(*wqty)++;
	memcpy(*dst, src, srcLen);
	(*dstSize) -= srcLen;
	(*writed) += srcLen;
	(*dst) += srcLen;
	return 0;
}
//-----------------------------------------------------------------------------
int EdfWriteInfRecData(EdfWriter_t* dw, const TypeRec_t* ir, const void* d, size_t len)
{
	int err;
	size_t writed = 0;
	if ((err = EdfWriteInfo(dw, ir, &writed)) ||
		(err = EdfWriteDataBlock(dw, d, len)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
int EdfWriteInfData0(EdfWriter_t* dw, PoType pt, uint32_t id, char* name, char* desc, const void* d)
{
	TypeRec_t rec = { { pt }, id, name, desc };
	return EdfWriteInfRecData(dw, &rec, d, GetTypeCSize(&rec.Inf));
}
//-----------------------------------------------------------------------------
int EdfWriteInfData(EdfWriter_t* dw, uint32_t id, PoType pt, char* name, const void* d)
{
	TypeRec_t rec = { { pt }, id, name };
	return EdfWriteInfRecData(dw, &rec, d, GetTypeCSize(&rec.Inf));
}
//-----------------------------------------------------------------------------
