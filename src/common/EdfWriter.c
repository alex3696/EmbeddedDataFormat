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
int EdfWriteConfig(EdfWriter_t* dw, size_t* writed)
{
	if (!dw->impl->WriteConfig)
		return ERR_FN_NOT_EXIST;
	int err = (*dw->impl->WriteConfig)(dw, &dw->Cfg, writed);
	if (err)
		return err;
	dw->Blk->Len = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int EdfWriteConfigBin(EdfWriter_t* dw, const EdfConfig_t* h, size_t* writed)
{
	dw->Blk->Type = (uint8_t)btConfig;
	dw->Blk->Len = (uint16_t)sizeof(EdfConfig_t);
	memcpy(&dw->Blk->Conent.Config, h, sizeof(EdfConfig_t));
	return EdfWriteBlockBin(&dw->Stream, h, (EdfBlock_t*)&dw->Blk->Type, writed);
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

	if (!dw->impl->WriteSchema || !t)
		return ERR_FN_NOT_EXIST;
	err = (*dw->impl->WriteSchema)(dw, t, writed);
	if (err)
		return err;
	dw->SchemaPtr = t;
	dw->BufLen = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int EdfWriteSchemaBin(EdfWriter_t* dw, const EdfSchema_t* t, size_t* writed)
{
	int err = 0;
	dw->Blk->Type = (uint8_t)btSchema;
	MemStream_t ms = { 0 };
	size_t w = 0;
	if ((err = MemStreamOutOpen(&ms, dw->Blk->Conent.Schema.Data, dw->SchMaxLen)) ||
		(err = WriteSchemaBinToStream((Stream_t*)&ms, t, &w)))
		return err;
	dw->Blk->Len = (uint16_t)w;// (uint16_t)ms.WPos;
	if ((err = EdfWriteBlockBin(&dw->Stream, &dw->Cfg, (EdfBlock_t*)&dw->Blk->Type, writed)))
		return err;
	dw->Blk->Conent.Record.SchId = t->Id;
	dw->PrimSkip = dw->Blk->Conent.Record.PrmOffset = 0;
	dw->RecordId = dw->Blk->Conent.Record.RecId = 0;
	dw->Blk->Len = 0;
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
	if (NULL == dw->impl->FlushData || 0 == dw->Blk->Len)
		return 0;
	int err = (*dw->impl->FlushData)(dw, writed);
	if (err)
		return err;
	dw->Blk->Len = 0;
	return err;
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataBin(EdfWriter_t* dw, size_t* writed)
{
	dw->Blk->Type = (uint8_t)btData;
	if (btData == dw->Blk->Type)
	{
		dw->Blk->Len += offsetof(EdfRecordContent_t, Data);
		//dw->Blk->Conent.Record.SchId = dw->SchemaPtr->Id;
	}
	int err = EdfWriteBlockBin(&dw->Stream, &dw->Cfg, (EdfBlock_t*)&dw->Blk->Type, writed);
	if (err)
		return err;
	//dw->Blk->Conent.Record.SchId = dw->SchemaPtr->Id;
	dw->Blk->Conent.Record.PrmOffset = dw->PrimSkip;
	dw->Blk->Conent.Record.RecId = dw->RecordId;
	return err;
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataTxt(EdfWriter_t* dw, size_t* writed)
{
	return StreamWrite((Stream_t*)&dw->Stream, writed, dw->Blk->Conent.Record.Data, dw->Blk->Len);
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static int SeekEnd(EdfWriter_t* f)
{
	int err = 0;
	while (!(err = EdfReadBlock(f)))
	{
		switch (f->Blk->Type)
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
	}//while
	if (ERR_EOF == err)
		err = 0;
	return err;
}
//-----------------------------------------------------------------------------
EdfWriter_t* EdfCreate(uint8_t* pMem, size_t memLen, const EdfConfig_t* pCfg, int* pErr)
{
	EdfWriter_t* pEdf;
	pEdf = (EdfWriter_t*)pMem;
	pMem +=	sizeof(EdfWriter_t);
	memLen -= sizeof(EdfWriter_t);
	if (pErr)
		*pErr = EdfInit(pEdf, pMem, memLen, pCfg);
	else
		EdfInit(pEdf, pMem, memLen, pCfg);
	return pEdf;
}
//-----------------------------------------------------------------------------
int EdfInit(EdfWriter_t* pEdf, uint8_t* pMem, size_t memLen, const EdfConfig_t* pCfg)
{ 
	int err = 0;
	if (NULL == pEdf)
		return ERR_WRONG_PARAMETERS;
	if (NULL == pMem)
		return ERR_WRONG_PARAMETERS;
	if (NULL == pCfg)
		return ERR_WRONG_PARAMETERS;
	const EdfConfig_t cfg = { EDF_VERSMAJOR,EDF_VERSMINOR, EDF_ENCODING, MIN_BLOCK_SIZE, Default };
	const size_t bufLen = (NULL == pCfg) ? cfg.Blocksize : pCfg->Blocksize;
	if (bufLen * 2 > memLen)
		return ERR_WRONG_PARAMETERS;

	pEdf->Cfg = (NULL == pCfg)? cfg : *pCfg;

	*(size_t*)&pEdf->SchMaxLen = bufLen - offsetof(EdfBlock_t, Conent) - 2;
	*(size_t*)&pEdf->RecMaxLen = bufLen - offsetof(EdfBlock_t, Conent) - 2 - offsetof(EdfRecordContent_t, Data);
	*(EdfBlock_t**)&pEdf->Blk = (EdfBlock_t*)pMem;

	*(size_t*)&pEdf->BufMaxLen = bufLen;
	*(uint8_t**)&pEdf->Buf = (uint8_t*)(pMem + bufLen);

	return err;
}
//-----------------------------------------------------------------------------
const EdfImpl_t writeBinToBin =
{
	.WritePrimitive = BinToBin,
	.WriteConfig = EdfWriteConfigBin,
	.WriteSchema = EdfWriteSchemaBin,
	.FlushData = StreamWriteBlockDataBin
};
const EdfImpl_t writeCBinToBin =
{
	.WritePrimitive = CBinToBin,
	.WriteConfig = EdfWriteConfigBin,
	.WriteSchema = EdfWriteSchemaBin,
	.FlushData = StreamWriteBlockDataBin
};
const EdfImpl_t writeBinToTxt =
{
	.WritePrimitive = BinToStr,
	.WriteConfig = EdfWriteConfigTxt,
	.WriteSchema = EdfWriteSchemaTxt,
	.FlushData = StreamWriteBlockDataTxt,
	.BeginStruct = SepBeginStruct,
	.EndStruct = SepEndStruct,
	.BeginArray = SepBeginArray,
	.EndArray = SepEndArray,
	.SepVarEnd = SepVarEnd,
	.RecBegin = SepRecBegin,
	.RecEnd = SepRecEnd,
};
const EdfImpl_t writeCBinToTxt =
{
	.WritePrimitive = CBinToStr,
	.WriteConfig = EdfWriteConfigTxt,
	.WriteSchema = EdfWriteSchemaTxt,
	.FlushData = StreamWriteBlockDataTxt,
	.BeginStruct = SepBeginStruct,
	.EndStruct = SepEndStruct,
	.BeginArray = SepBeginArray,
	.EndArray = SepEndArray,
	.SepVarEnd = SepVarEnd,
	.RecBegin = SepRecBegin,
	.RecEnd = SepRecEnd,
};
const EdfImpl_t readBinToCBin =
{
	.WritePrimitive = BinToBin,
};
const EdfImpl_t readTxtToCBin =
{
	//.WritePrimitive = StrToBin,
};

//-----------------------------------------------------------------------------
int EdfOpenStream(EdfWriter_t* f, Stream_t* stream, const char* mode)
{
	if (2 > strnlength(mode, 2))
		return ERR_WRONG_PARAMETERS;
	int err = 0;
	f->SchemaPtr = NULL;

	if (0 == strncmp("wb", mode, 2) || 0 == strncmp("ab", mode, 2))
	{
		f->Stream = *stream;
		f->BufLen = 0;
		f->impl = strchr(mode, 'c') ? &writeBinToBin : &writeCBinToBin;
		if (strchr(mode, 'a'))
		{
			err = SeekEnd(f);
		}
	}
	else if (0 == strncmp("wt", mode, 2) || 0 == strncmp("at", mode, 2))
	{
		f->Stream = *stream;
		f->BufLen = 0;
		f->impl = strchr(mode, 'c') ? &writeBinToTxt : &writeCBinToTxt;
		if (strchr(mode, 'a'))
		{
			err = StreamSeek(stream, 0, FSEEK_END);
		}
		return err;
	}
	else if (0 == strncmp("rb", mode, 2))
	{
		f->Stream = *stream;
		f->BufLen = 0;
		f->impl = &readBinToCBin;
	}
	if (0 == strncmp("rt", mode, 2))
	{
		f->Stream = *stream;
		f->BufLen = 0;
		f->impl = &readTxtToCBin;
		err = ERR_WRONG_PARAMETERS;
	}
	return err;
}
//-----------------------------------------------------------------------------
int EdfOpenFile(EdfWriter_t* edf, const char* file, const char* mode)
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
