#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int EdfWriteBlockBin(EdfContext_t* dw, size_t* writed)
{
	EdfBlock_t* blk = dw->Blk;
	int err = 0;
	uint16_t* blkCrc = (uint16_t*)((uint8_t*)blk + EDF_HEADER_SIZE + blk->Len);
	*blkCrc = MbCrc16(blk, EDF_HEADER_SIZE + blk->Len);
	if ((err = StreamWrite(&dw->Stream, NULL, blk, EDF_HEADER_SIZE + blk->Len + EDF_CRC_SIZE)))
		return err;
	if(writed)
		*writed = blk->Len;
	dw->BlkQty++;
	return ERR_NO;
}
//-----------------------------------------------------------------------------

// Write Config
//-----------------------------------------------------------------------------
int EdfWriteConfig(EdfContext_t* dw, size_t* writed)
{
	if (!dw->WriteConfig)
		return ERR_FN_NOT_EXIST;
	int err = 0;
	if ((err = (*dw->WriteConfig)(dw, &dw->Cfg, writed)))
		return err;
	dw->Blk->Len = 0;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
static int EdfWriteConfigBin(EdfContext_t* dw, const EdfConfig_t* h, size_t* writed)
{
	dw->Blk->Type = (uint8_t)btConfig;
	dw->Blk->Len = (uint16_t)sizeof(EdfConfig_t);
	memcpy(&dw->Blk->Content.Config, h, sizeof(EdfConfig_t));
	return EdfWriteBlockBin(dw, writed);
}
//-----------------------------------------------------------------------------
static int EdfWriteConfigTxt(EdfContext_t* dw, const EdfConfig_t* h, size_t* writed)
{
	int err = 0;
	const char info[] = "//Edf Config: VersMajor; VersMinor; Blocksize; Encoding; Flags\n";
	if ((err = StreamWrite(&dw->Stream, writed, info, sizeof(info) - 1)))
		return err;

	if (   (err = StreamWrite(&dw->Stream, writed, "<~{", 3))
		|| (err = StreamWriteUInt32Txt(&dw->Stream, writed, h->VersMajor))
		|| (err = StreamWrite(&dw->Stream, writed, ";", 1))
		|| (err = StreamWriteUInt32Txt(&dw->Stream, writed, h->VersMinor))
		|| (err = StreamWrite(&dw->Stream, writed, ";", 1))
		|| (err = StreamWriteUInt32Txt(&dw->Stream, writed, h->Blocksize))
		|| (err = StreamWrite(&dw->Stream, writed, ";", 1))
		|| (err = StreamWriteUInt32Txt(&dw->Stream, writed, h->Encoding))
		|| (err = StreamWrite(&dw->Stream, writed, ";", 1))
		|| (err = StreamWriteUInt32Txt(&dw->Stream, writed, h->Flags))
		|| (err = StreamWrite(&dw->Stream, writed, ";}>\n", 4)) )
		return err;
	return ERR_NO;
}

// Write Schema
//-----------------------------------------------------------------------------
int EdfWriteSchema(EdfContext_t* dw, const EdfSchema_t* t, size_t* writed)
{
	int err = 0;
	size_t flushed = 0;
	if ((err = EdfFlushData(dw, &flushed)))
		return err;
	if (!dw->WriteSchema || !t)
		return ERR_FN_NOT_EXIST;
	if ((err = (*dw->WriteSchema)(dw, t, writed)))
		return err;
	//кешируем схему в оперативку для быстрого доступа при записи
	//ранее просто хранил указатель (который мог быть в памяти программ)
	//для микроконтроллера это обращение накладно 
	if ((err = SchemaCopyСBinToCBin(t, (EdfSchema_t**)&dw->SchemaPtr, dw->Buf, dw->Cfg.Blocksize, &dw->BufLen)))
		return err;
	if (dw->WritePrimitive == CBinToBin)
		GetTypeInfo(&dw->SchemaPtr->Type, &dw->TypeCSize, &dw->HasDynamicFields);
	else
		dw->TypeCSize = dw->HasDynamicFields = 0;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
/**
 * Записывает блок схемы (btSchema) в поток.
 *
 * ВНИМАНИЕ: После успешной записи схемы автоматически инициализирует
 * состояние для последующей записи блоков данных (btData):
 *   - Устанавливает SchId в заголовке для всех следующих блоков данных
 *   - Сбрасывает PrimSkip и RecordId в 0 (начало новой последовательности записей)
 *   - Обнуляет счетчик длины текущего блока данных
 *
 * Таким образом, вызов EdfWriteSchema подготавливает EdfWriter_t
 * к немедленной записи данных через EdfWriteData.
 *
 * @param dw      Указатель на EdfWriter_t
 * @param t       Указатель на схему (EdfSchema_t)
 * @param writed  Куда записать количество записанных байт (может быть NULL)
 * @return        Код ошибки или ERR_NO при успехе
 */
static int EdfWriteSchemaBin(EdfContext_t* dw, const EdfSchema_t* t, size_t* writed)
{
	int err = 0;
	dw->Blk->Type = (uint8_t)btSchema;
	MemStream_t ms = { 0 };
	size_t w = 0;
	if ((err = MemStreamWriteOpen(&ms, dw->Blk->Content.Schema.Data, GetContentDataMaxLen(dw, btSchema))) ||
		(err = WriteSchemaBinToStream((Stream_t*)&ms, t, &w)))
		return err;
	dw->Blk->Len = (uint16_t)w;// (uint16_t)ms.WPos;
	if ((err = EdfWriteBlockBin(dw, writed)))
		return err;
	// --- ИНИЦИАЛИЗАЦИЯ СОСТОЯНИЯ ДЛЯ СЛЕДУЮЩИХ БЛОКОВ ДАННЫХ ---
	  // Устанавливаем Id схемы в заголовок для будущих блоков данных
	dw->Blk->Content.Record.SchId = t->Id;
	// Сброс счетчика примитивов (начинаем с первого примитива новой записи)
	dw->PrimSkip = dw->Blk->Content.Record.PrmOffset = 0;
	// Сброс номера записи (первая запись будет иметь номер 0)
	dw->RecordId = dw->Blk->Content.Record.RecId = 0;
	// Сброс длины данных для нового блока
	dw->Blk->Len = 0;
	return 0;
}
//-----------------------------------------------------------------------------
static int EdfWriteSchemaTxt(EdfContext_t* w, const EdfSchema_t* t, size_t* writed)
{
	return WriteSchemaTxtToStream(&w->Stream, t, writed);
}

// Write Data
//-----------------------------------------------------------------------------
int EdfFlushData(EdfContext_t* dw, size_t* writed)
{
	if (NULL == dw->FlushData || 0 == dw->Blk->Len)
		return 0;
	int err = 0;
	if ((err = (*dw->FlushData)(dw, writed)))
		return err;
	dw->Blk->Len = 0;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
/**
 * Записывает блок данных в бинарном режиме.
 *
 * ВНИМАНИЕ: Поля PrmOffset и RecId в заголовке блока устанавливаются
 * ДО вызова этой функции следующий раз.
 *
 * При разрыве примитива между блоками:
 * - PrmOffset указывает номер примитива, с которого нужно продолжить чтение
 * - RecId остается неизменным для незавершенной записи
 *
 * При успешной записи целой записи:
 * - PrmOffset = 0 (начало новой записи)
 * - RecId инкрементируется
 */
static int StreamWriteBlockDataBin(EdfContext_t* dw, size_t* writed)
{
	dw->Blk->Type = (uint8_t)btData;
	// На момент вызова dw->Blk->Len содержит только длину поля Data
	// добавляем размер заголовка (8 байт) к Len и записывает блок
	dw->Blk->Len += offsetof(EdfRecordContent_t, Data);
	int err = 0;
	if ((err = EdfWriteBlockBin(dw, writed)))
		return err;
	//dw->Blk->Content.Record.SchId = dw->SchemaPtr->Id;
	dw->Blk->Content.Record.PrmOffset = dw->PrimSkip;
	dw->Blk->Content.Record.RecId = dw->RecordId;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
static int StreamWriteBlockDataTxt(EdfContext_t* dw, size_t* writed)
{
	return StreamWrite((Stream_t*)&dw->Stream, writed, dw->Blk->Content.Record.Data, dw->Blk->Len);
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static int SeekEnd(EdfContext_t* f)
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
EdfContext_t* EdfCreate(uint8_t* pMem, size_t memLen, const EdfConfig_t* pCfg, int* pErr)
{
	EdfContext_t* pEdf;
	pEdf = (EdfContext_t*)pMem;
	pMem +=	sizeof(EdfContext_t);
	memLen -= sizeof(EdfContext_t);
	if (pErr)
		*pErr = EdfInit(pEdf, pMem, memLen, pCfg);
	else
		EdfInit(pEdf, pMem, memLen, pCfg);
	return pEdf;
}
//-----------------------------------------------------------------------------
int EdfInit(EdfContext_t* pEdf, uint8_t* pMem, size_t memLen, const EdfConfig_t* pCfg)
{ 
	//int err = 0;
	if (NULL == pEdf)
		return ERR_WRONG_PARAMETERS;
	if (NULL == pMem)
		return ERR_WRONG_PARAMETERS;
	const EdfConfig_t* const cfg = (NULL == pCfg) ? &EdfCfg256 : pCfg;
	if (cfg->VersMajor != EDF_VERSMAJOR || cfg->VersMinor != EDF_VERSMINOR)
		return ERR_WRONG_PARAMETERS;
	if (cfg->Blocksize < MIN_BLOCK_SIZE || cfg->Blocksize > MAX_BLOCK_SIZE)
		return ERR_WRONG_PARAMETERS;
	const size_t bufLen = cfg->Blocksize;
	if (bufLen > memLen / 2)
		return ERR_WRONG_PARAMETERS;
	memset((void*)pEdf, 0, sizeof(EdfContext_t));
	pEdf->Cfg = *cfg;
	*(EdfBlock_t**)&pEdf->Blk = (EdfBlock_t*)pMem;
	*(uint8_t**)&pEdf->Buf = (uint8_t*)(pMem + bufLen);
	return 0;
}
//-----------------------------------------------------------------------------
int EdfOpenStream(EdfContext_t* f, Stream_t* stream, const char* mode)
{
	if (2 > strnlength(mode, 2))
		return ERR_WRONG_PARAMETERS;
	int err = 0;
	f->SchemaPtr = NULL;
	switch (stream->Impl.TypeId)
	{
		default: return ERR_WRONG_PARAMETERS;
		case T_FILE_STREAM: memcpy(&f->Stream, stream, sizeof(FileStream_t)); break;
		case T_MEM_STREAM: memcpy(&f->Stream, stream, sizeof(MemStream_t)); break;
	}
	f->BufLen = 0;
	f->WritePrimitive = NULL;
	f->WriteConfig = NULL;
	f->WriteSchema = NULL;
	f->FlushData = NULL;
	if (0 == strncmp("wb", mode, 2) || 0 == strncmp("ab", mode, 2))
	{
		f->WritePrimitive = CBinToBin;
		f->WriteConfig = EdfWriteConfigBin;
		f->WriteSchema = EdfWriteSchemaBin;
		f->FlushData = StreamWriteBlockDataBin;
		if (strchr(mode, 'a'))
		{
			err = SeekEnd(f);
		}
	}
	else if (0 == strncmp("rb", mode, 2))
	{
		f->WritePrimitive = BinToBin;
	}
#ifdef EDF_ENABLE_TEXT_MODE
	else if (0 == strncmp("wt", mode, 2) || 0 == strncmp("at", mode, 2))
	{
		f->WritePrimitive = CBinToStr;
		f->WriteConfig = EdfWriteConfigTxt;
		f->WriteSchema = EdfWriteSchemaTxt;
		f->FlushData = StreamWriteBlockDataTxt;
		if (strchr(mode, 'a'))
		{
			err = StreamSeek(stream, 0, FSEEK_END);
		}
		return err;
	}
#else
	else if (0 == strncmp("wt", mode, 2) || 0 == strncmp("at", mode, 2) || 0 == strncmp("rt", mode, 2))
	{
		err = ERR_FN_NOT_EXIST;
	}
#endif
	return err;
}
//-----------------------------------------------------------------------------
int EdfOpenFile(EdfContext_t* edf, const char* file, const char* mode)
{
	return EdfOpenWithFs(edf, file, mode, FileStreamOpen);
}
//-----------------------------------------------------------------------------
int EdfOpenWithFs(EdfContext_t* edf, const char* file, const char* mode, FileStreamOpenFn fnOpen)
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
int EdfClose(EdfContext_t* dw)
{
	size_t w = 0;
	int err0 = EdfFlushData(dw, &w);
	int err1 = StreamClose(&dw->Stream);
	return 0 != err0? err0 : err1;
}
//-----------------------------------------------------------------------------
int EdfWriteSchemaData(EdfContext_t* dw, const EdfSchema_t* ir, const void* d, size_t len)
{
	int err;
	size_t writed = 0;
	if ((err = EdfWriteSchema(dw, ir, &writed)) ||
		(err = EdfWriteData(dw, d, len, NULL)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
int EdfWritePrimSchData(EdfContext_t* dw, PoType pt, uint16_t schId, char* schName, char* schDesc, const void* d)
{
	EdfSchema_t rec = { schId, schName, schDesc, { pt } };
	return EdfWriteSchemaData(dw, &rec, d, GetTypeCSize(&rec.Type));
}
//-----------------------------------------------------------------------------
