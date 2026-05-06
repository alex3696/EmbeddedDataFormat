#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int EdfWriteBlockBin(Stream_t* st, const EdfConfig_t* cfg, const EdfBlock_t* blk, size_t* writed)
{
	UNUSED(cfg);
	int err = 0;
	uint16_t* blkCrc = (uint16_t*)((uint8_t*)blk + 3 + blk->Len);
	*blkCrc = MbCrc16(blk, 3 + blk->Len);
	if ((err = StreamWrite(st, NULL, blk, 3 + blk->Len + 2)))
		return err;
	*writed = blk->Len;
	return 0;
}
//-----------------------------------------------------------------------------

// Write Config
//-----------------------------------------------------------------------------
int EdfWriteConfig(EdfWriter_t* dw, const EdfConfig_t* h, size_t* writed)
{
	if (!dw->WriteConfig || !h)
		return ERR_FN_NOT_EXIST;
	dw->Cfg = *h;
	int err = (*dw->WriteConfig)(dw, h, writed);
	if (err)
	{
		LOG_ERR();
		return err;
	}
	dw->Blk.Len = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int EdfWriteConfigBin(EdfWriter_t* dw, const EdfConfig_t* h, size_t* writed)
{
	dw->Blk.Type = (uint8_t)btConfig;
	dw->Blk.Len = (uint16_t)sizeof(EdfConfig_t);
	memcpy(dw->Blk.Data, h, sizeof(EdfConfig_t));
	return EdfWriteBlockBin(&dw->Stream, h, (EdfBlock_t*)&dw->Blk.Type, writed);
}
//-----------------------------------------------------------------------------
static int EdfWriteConfigTxt(EdfWriter_t* dw, const EdfConfig_t* h, size_t* writed)
{
	return StreamWriteFmt(&dw->Stream, writed, "<~ {version=%d.%d; bs=%d; encoding=%d; flags=%d; } >\n"
		, h->VersMajor, h->VersMinor
		, h->Blocksize, h->Encoding, h->Flags);
}

// Write Schema
//-----------------------------------------------------------------------------
int EdfWriteSchema(EdfWriter_t* dw, const EdfSchema_t* t, size_t* writed)
{
	int err = 0;
	size_t flushed = 0;
	if ((err = EdfFlushData(dw, &flushed)))
		return err;
	dw->Skip = 0;
	dw->RecordId = 0;

	if (!dw->WriteSchema || !t)
		return ERR_FN_NOT_EXIST;
	err = (*dw->WriteSchema)(dw, t, writed);
	if (err)
	{
		LOG_ERR();
		return err;
	}
	dw->SchemaPtr = t;
	dw->Blk.Len = 0;
	dw->BufLen = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int EdfWriteSchemaBin(EdfWriter_t* dw, const EdfSchema_t* t, size_t* writed)
{
	int err = 0;
	dw->Blk.Type = (uint8_t)btSchema;
	MemStream_t ms = { 0 };
	size_t w = 0;
	if ((err = MemStreamOutOpen(&ms, dw->Blk.Data, sizeof(dw->Blk.Data))) ||
		(err = WriteSchemaBinToStream((Stream_t*)&ms, t, &w)))
		return err;
	dw->Blk.Len = (uint16_t)w;// (uint16_t)ms.WPos;
	if ((err = EdfWriteBlockBin(&dw->Stream, &dw->Cfg, (EdfBlock_t*)&dw->Blk.Type, writed)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
static int EdfWriteSchemaTxt(EdfWriter_t* w, const EdfSchema_t* t, size_t* writed)
{
	return WriteSchemaTxtToStream(&w->Stream, t, writed);
}

// Write Data
//-----------------------------------------------------------------------------
int EdfFlushData(EdfWriter_t* dw, size_t* writed)
{
	if (NULL == dw->FlushData || 0 == dw->Blk.Len)
		return 0;
	int err = (*dw->FlushData)(dw, writed);
	if (err)
	{
		LOG_ERR();
		return err;
	}
	dw->Blk.Len = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataBin(EdfWriter_t* dw, size_t* writed)
{
	dw->Blk.Type = (uint8_t)btData;
	return EdfWriteBlockBin(&dw->Stream, &dw->Cfg, (EdfBlock_t*)&dw->Blk.Type, writed);
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataTxt(EdfWriter_t* dw, size_t* writed)
{
	return StreamWrite((Stream_t*)&dw->Stream, writed, dw->Blk.Data, dw->Blk.Len);
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static int SeekEnd(EdfWriter_t* f)
{
	int err = 0;
	while (!(err = EdfReadBlock(f)))
	{
		switch (f->Blk.Type)
		{
		default: break;
		case btConfig: break;
		case btSchema:
		{
		}
		break;
		case btData:
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
	f->SchemaPtr = NULL;
	f->Cfg = MakeDefaultConfig();
	if (0 == strncmp("wb", mode, 2) || 0 == strncmp("ab", mode, 2))
	{
		f->Stream = *stream;
		f->Skip = 0;
		f->RecordId = 0;
		f->Blk.Len = 0;
		f->BufLen = 0;
		f->WritePrimitive = strchr(mode, 'c') ? BinToBin : CBinToBin;
		f->WriteConfig = EdfWriteConfigBin;
		f->WriteSchema = EdfWriteSchemaBin;
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
		f->Skip = 0;
		f->RecordId = 0;
		f->Blk.Len = 0;
		f->BufLen = 0;
		f->WritePrimitive = strchr(mode, 'c') ? BinToStr : CBinToStr;
		f->WriteConfig = EdfWriteConfigTxt;
		f->WriteSchema = EdfWriteSchemaTxt;
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
		f->Skip = 0;
		f->RecordId = 0;
		f->Blk.Len = 0;
		f->BufLen = 0;
		f->WritePrimitive = BinToBin;
		f->WriteConfig = NULL;
		f->WriteSchema = NULL;
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
	if ((err = EdfFlushData(dw, &w)))
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
int EdfWriteSchemaData(EdfWriter_t* dw, const EdfSchema_t* ir, const void* d, size_t len)
{
	int err;
	size_t writed = 0;
	if ((err = EdfWriteSchema(dw, ir, &writed)) ||
		(err = EdfWriteData(dw, d, len)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
int EdfWritePrimSchData(EdfWriter_t* dw, PoType pt, uint16_t schId, char* schName, char* schDesc, const void* d)
{
	EdfSchema_t rec = { schId, schName, schDesc, { pt } };
	return EdfWriteSchemaData(dw, &rec, d, GetTypeCSize(&rec.Type));
}
//-----------------------------------------------------------------------------
