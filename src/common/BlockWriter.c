#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int EdfWriteSep(EdfContext_t* edf, const char* const sep)
{
	// игнорируем любые разделители, если мы не в текстовом режиме
	if(edf->WritePrimitive == CBinToBin)
		return 0;

	if (0 < edf->PrimSkip)
	{
		edf->PrimSkip--;
		return 0;
	}
	size_t sepLen = sep ? strnlength(sep, 10) : 0;
	if (!sepLen)
	{
		edf->wqty++;
		return 0;
	}
	if (sepLen > edf->WalkCtx.dstLen)
	{
		int err = 0;
		edf->Blk->Len += (uint16_t)(edf->WalkCtx.writed);
		if ((err = EdfFlushData(edf, &edf->WalkCtx.writed)))
			return err;
		edf->WalkCtx.writed = 0;
		edf->WalkCtx.dstLen = GetDataMaxLen(edf->Cfg.Blocksize);
		edf->WalkCtx.pdst = edf->Blk->Content.Record.Data;
		if (sepLen > edf->WalkCtx.dstLen)
			return ERR_DST_SHORT;
	}
	edf->wqty++;
	memcpy(edf->WalkCtx.pdst, sep, sepLen);
	edf->WalkCtx.dstLen -= sepLen;
	edf->WalkCtx.writed += sepLen;
	edf->WalkCtx.pdst += sepLen;
	return 0;
}

#define EdfWriteSepBeginStruct(WalkCtx) (EdfWriteSep(WalkCtx, SepBeginStruct))
#define EdfWriteSepEndStruct(WalkCtx) (EdfWriteSep(WalkCtx, SepEndStruct))
#define EdfWriteSepBeginArray(WalkCtx) (EdfWriteSep(WalkCtx, SepBeginArray))
#define EdfWriteSepEndArray(WalkCtx) (EdfWriteSep(WalkCtx, SepEndArray))
#define EdfWriteSepRecBegin(WalkCtx) (EdfWriteSep(WalkCtx, SepRecBegin))
#define EdfWriteSepRecEnd(WalkCtx) (EdfWriteSep(WalkCtx, SepRecEnd))
#define EdfWriteSepVarEnd(WalkCtx) (EdfWriteSep(WalkCtx, SepVarEnd))

//-----------------------------------------------------------------------------
// 
static int WriteOnePrimitive(const EdfType_t* t, EdfContext_t* edf)
{
	if (0 < edf->PrimSkip)
	{
		edf->PrimSkip--;
		return ERR_NO;
	}
	int err = 0;
	if ((err = (edf->WritePrimitive)(t->Type, &edf->WalkCtx)))
	{
		if (ERR_DST_SHORT != err)
			return err;
		// Сбрасываем блок
		edf->Blk->Len += (uint16_t)(edf->WalkCtx.writed);
		edf->PrimSkip = edf->wqty;
		size_t writed = 0;
		if ((err = EdfFlushData(edf, &writed)))
			return err;
		edf->PrimSkip = 0;
		// Сбрасываем счетчики для нового блока
		edf->WalkCtx.writed = 0;
		edf->WalkCtx.dstLen = GetDataMaxLen(edf->Cfg.Blocksize);
		edf->WalkCtx.pdst = edf->Blk->Content.Record.Data;
		// Пытаемся записать ЕЩЕ РАЗ
		if ((err = (edf->WritePrimitive)(t->Type, &edf->WalkCtx)))
			return err;// если снова ошибка, выходим
	}
	edf->wqty++;
	return err;
}
//-----------------------------------------------------------------------------
static int WriteChar(const EdfType_t* t, EdfContext_t* edf)
{
	if (0 < edf->PrimSkip)
	{
		edf->PrimSkip--;
		return ERR_NO;
	}
	int err = ERR_NO;
	size_t srcLen = edf->WalkCtx.srcLen;
	size_t totalChars = GetTotalElements((EdfDims_t*)&t->Dims);
	if (srcLen < totalChars)
		return ERR_SRC_SHORT;
	edf->WalkCtx.srcLen = totalChars;
	if ((err = WriteOnePrimitive(t, edf)))
	{
		edf->WalkCtx.srcLen = srcLen;
		return err;
	}
	edf->WalkCtx.srcLen = srcLen - totalChars;
	return err;
}
#ifdef EDF_ENABLE_TEXT_MODE
//-----------------------------------------------------------------------------
static int WriteElement(const EdfType_t* t, EdfContext_t* edf)
{
	int err = ERR_NO;
	if (Char == t->Type)
	{
		if ((err = WriteChar(t, edf)))
			return err;
		return EdfWriteSepVarEnd(edf);
	}
	size_t totalElement = GetTotalElements((EdfDims_t*)&t->Dims);
	if (1 < totalElement)
	{
		if ((err = EdfWriteSepBeginArray(edf)))
			return err;
	}
	if (Struct == t->Type)
	{
		for (size_t i = 0; i < totalElement; i++)
		{
			if ((err = EdfWriteSepBeginStruct(edf)))
				return err;
			for (size_t j = 0; j < t->Fields.Count; j++)
			{
				if ((err = WriteElement(&t->Fields.Item[j], edf)))
					return err;
			}
			if ((err = EdfWriteSepEndStruct(edf)))
				return err;
		}
	}
	else
	{
		for (size_t i = 0; i < totalElement; i++)
		{
			if ((err = WriteOnePrimitive(t, edf)))
				return err;
			if ((err = (EdfWriteSepVarEnd(edf))))
				return err;
		}
	}
	if (1 < totalElement)
	{
		if ((err = (EdfWriteSepEndArray(edf))))
			return err;
	}
	return ERR_NO;
}
#else
static int WriteElement(const EdfType_t* t, EdfContext_t* edf)
{
	int err = ERR_NO;
	if (Struct == t->Type)
	{
		size_t totalElement = GetTotalElements((EdfDims_t*)&t->Dims);
		while(totalElement--)
		{
			for (size_t j = 0; j < t->Fields.Count; j++)
			{
				if ((err = WriteElement(&t->Fields.Item[j], edf)))
					return err;
			}
		}
	}
	else
	{
		if (Char == t->Type)
			return WriteChar(t, edf);
		size_t totalElement = GetTotalElements((EdfDims_t*)&t->Dims);
		while (totalElement--)
		{
			if ((err = WriteOnePrimitive(t, edf)))
				return err;
		}
	}
	return ERR_NO;
}
#endif
//-----------------------------------------------------------------------------
#ifdef EDF_ENABLE_TEXT_MODE
static int WriteSingleValue(EdfContext_t* edf)
{
	int err;
	if ((err = EdfWriteSepRecBegin(edf)))
		return err;
	if ((err = WriteElement(&edf->SchemaPtr->Type, edf)))
		return err;
	if ((err = EdfWriteSepRecEnd(edf)))
		return err;
	return err;
}
#endif
//-----------------------------------------------------------------------------
int EdfWriteDataParse(EdfContext_t* dw, const void* vsrc, size_t xsrcLen, size_t* srcConsumed)
{
	//if (dw == NULL || NULL == dw->SchemaPtr)
	//	return ERR_WRONG_TYPE;
	//if (dw->WritePrimitive == NULL)  // режим чтения
	//	return ERR_WRONG_PARAMETERS;
	//if (vsrc == NULL || xsrcLen == 0)
	//	return ERR_NO;
	//if (srcConsumed != NULL)
	//	*srcConsumed = 0;
	dw->WalkCtx.psrc = (const uint8_t*)vsrc;
	dw->WalkCtx.srcLen = xsrcLen;
	dw->WalkCtx.dstLen = GetDataMaxLen(dw->Cfg.Blocksize) - dw->Blk->Len;
	dw->WalkCtx.pdst = dw->Blk->Content.Record.Data + dw->Blk->Len;

	int wr;
	do
	{
		dw->WalkCtx.writed = dw->WalkCtx.readed = 0;
		dw->wqty = dw->PrimSkip;
#ifdef EDF_ENABLE_TEXT_MODE
		wr = WriteSingleValue(dw);
#else
		wr = WriteElement(&dw->SchemaPtr->Type, dw);
#endif
		// Увеличиваем размер занятых данных в текущем блоке на то, что вернул WriteSingleValue.
		dw->Blk->Len += (uint16_t)dw->WalkCtx.writed;
		if (srcConsumed != NULL)
			*srcConsumed += dw->WalkCtx.readed;

		switch (wr)
		{
		default:
		case ERR_WRONG_TYPE: return ERR_WRONG_TYPE;
		case ERR_SRC_SHORT:
			// Входной буфер оборвался на середине примитива/массива.
			// Запоминаем позицию в схеме (wqty), чтобы при следующем вызове начать с нужного места.
			dw->PrimSkip = dw->wqty;
			return ERR_SRC_SHORT;
		case ERR_NO:
			dw->PrimSkip = 0;
			dw->RecordId++;
			break;
		case ERR_DST_SHORT:
			//dstLen = GetContentDataMaxLen(dw, btData);
			//dst = dw->Blk->Content.Record.Data;
			//dw->PrimSkip = (uint16_t)wqty;
			//wr = 0;
			return ERR_DST_SHORT;
			break;
		}
	} while (0 < dw->WalkCtx.srcLen);
	return wr;
}
//-----------------------------------------------------------------------------
//typedef struct
//{
//	uint8_t Raw[100];
//} Raw100_t;
//-----------------------------------------------------------------------------
int EdfWriteData(EdfContext_t* dw, const void* vsrc, size_t xsrcLen, size_t* srcConsumed)
{
	//Raw100_t r;
	if (dw == NULL || NULL == dw->SchemaPtr)
		return ERR_WRONG_TYPE;
	if (dw->WritePrimitive == NULL)  // режим чтения
		return ERR_WRONG_PARAMETERS;
	if (vsrc == NULL || xsrcLen == 0)
		return ERR_NO;
	if (srcConsumed != NULL)
		*srcConsumed = 0;

	if (dw->WritePrimitive == CBinToBin
		&& !dw->HasDynamicFields
		&& 0 == dw->PrimSkip
		&& xsrcLen >= dw->TypeCSize)
	{
		int err;
		const uint8_t* src = (uint8_t*)vsrc;
		const size_t structLen = dw->TypeCSize;
		size_t totalStructsCount = xsrcLen / structLen;
		size_t freeLen = (uint16_t)GetDataMaxLen(dw->Cfg.Blocksize) - dw->Blk->Len;
		size_t maxFullStructsCount = freeLen / structLen;
		while (totalStructsCount)
		{
			size_t currCount = totalStructsCount < maxFullStructsCount ? totalStructsCount : maxFullStructsCount;
			if (xsrcLen && 0 == currCount) // если полностью в блок структура не входит
			{
				if (0 < (dw->Cfg.Flags & DisableStructBlockTransfer))
				{
					if ((err = EdfFlushData(dw, NULL)))
						return err;
					freeLen = (uint16_t)GetDataMaxLen(dw->Cfg.Blocksize);
					maxFullStructsCount = freeLen / structLen;
					continue;
				}
				else
				{
					if ((err = EdfWriteDataParse(dw, (void*)src, structLen, srcConsumed)))
						return err;
					totalStructsCount -= 1;
					xsrcLen -= structLen;
					src += structLen;
					freeLen = (uint16_t)GetDataMaxLen(dw->Cfg.Blocksize) - dw->Blk->Len;
					maxFullStructsCount = freeLen / structLen;
					continue;
				}
			}
			size_t currLen = currCount * structLen;
			uint8_t* dst = dw->Blk->Content.Record.Data + dw->Blk->Len;
			memcpy(dst, src, currLen);
			dw->Blk->Len += (uint16_t)currLen;
			dw->RecordId += currCount;
			if (srcConsumed != NULL)
				*srcConsumed += currLen;
			totalStructsCount -= currCount;
			if(!totalStructsCount)
				return ERR_NO;
			xsrcLen -= currLen;
			src += currLen;
			freeLen = (uint16_t)GetDataMaxLen(dw->Cfg.Blocksize) - dw->Blk->Len;
			maxFullStructsCount = freeLen / structLen;
		}
	}
	else
		return EdfWriteDataParse(dw, vsrc, xsrcLen, srcConsumed);
	return ERR_NO;
}
