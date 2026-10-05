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
		edf->WalkCtx.dstLen = GetContentDataMaxLen(edf, btData);
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
	if (0 < (edf->PrimSkip))
	{
		edf->PrimSkip--;
		return ERR_NO;
	}
	size_t srcLen = 0;
	if (Char == t->Type)
	{
		srcLen = edf->WalkCtx.srcLen;
		if (0 == srcLen)
			return ERR_SRC_SHORT;
		if (srcLen > edf->WalkCtx.srcLen)
			return ERR_SRC_SHORT;
		edf->WalkCtx.srcLen = GetTotalElements((EdfDims_t*)&t->Dims);
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
		edf->WalkCtx.dstLen = GetContentDataMaxLen(edf, btData);
		edf->WalkCtx.pdst = edf->Blk->Content.Record.Data;
		// Пытаемся записать ЕЩЕ РАЗ
		if ((err = (edf->WritePrimitive)(t->Type, &edf->WalkCtx)))
			return err;// если снова ошибка, выходим
	}
	edf->wqty++;
	if (Char == t->Type)
	{
		edf->WalkCtx.srcLen = srcLen - GetTotalElements((EdfDims_t*)&t->Dims);
	}
	return err;
}
//-----------------------------------------------------------------------------
static int WriteElement(const EdfType_t* t, EdfContext_t* edf)
{
	int err = ERR_NO;
	if (Char == t->Type)
	{
		if ((err = WriteOnePrimitive(t, edf)))
			return err;
#ifdef EDF_ENABLE_TEXT_MODE
		return EdfWriteSepVarEnd(edf);
#else
		return err;
#endif
	}
	size_t totalElement = GetTotalElements((EdfDims_t*)&t->Dims);
#ifdef EDF_ENABLE_TEXT_MODE
	if (1 < totalElement)
	{
		if ((err = EdfWriteSepBeginArray(edf)))
			return err;
	}
#endif
	for (size_t i = 0; i < totalElement; i++)
	{
		if (Struct == t->Type)
		{
#ifdef EDF_ENABLE_TEXT_MODE
			if ((err = EdfWriteSepBeginStruct(edf)))
				return err;
#endif
			for (size_t j = 0; j < t->Fields.Count; j++)
			{
				const EdfType_t* s = &t->Fields.Item[j];
				if ((err = WriteElement(s, edf)))
					return err;
			}
#ifdef EDF_ENABLE_TEXT_MODE
			if ((err = EdfWriteSepEndStruct(edf)))
				return err;
#endif
		}
		else
		{
			if ((err = WriteOnePrimitive(t, edf)))
				return err;
#ifdef EDF_ENABLE_TEXT_MODE
			if ((err = (EdfWriteSepVarEnd(edf))))
				return err;
#endif
		}
	}
#ifdef EDF_ENABLE_TEXT_MODE
	if (1 < totalElement)
	{
		if ((err = (EdfWriteSepEndArray(edf))))
			return err;
	}
#endif
	return err;
}
//-----------------------------------------------------------------------------
static int WriteSingleValue(EdfContext_t* edf)
{
	int err;
#ifdef EDF_ENABLE_TEXT_MODE
	if ((err = EdfWriteSepRecBegin(edf)))
		return err;
#endif
	if ((err = WriteElement(&edf->SchemaPtr->Type, edf)))
		return err;
#ifdef EDF_ENABLE_TEXT_MODE
	if ((err = EdfWriteSepRecEnd(edf)))
		return err;
#endif
	return err;
}
//-----------------------------------------------------------------------------
int EdfWriteData(EdfContext_t* dw, const void* vsrc, size_t xsrcLen, size_t* srcConsumed)
{
	if (dw == NULL || NULL == dw->SchemaPtr)
		return ERR_WRONG_TYPE;
	if (dw->WritePrimitive == NULL)  // режим чтения
		return ERR_WRONG_PARAMETERS;
	if (vsrc == NULL || xsrcLen == 0)
		return ERR_NO;
	if (srcConsumed != NULL)
		*srcConsumed = 0;
	
	dw->WalkCtx.psrc = (const uint8_t*)vsrc;
	dw->WalkCtx.srcLen = xsrcLen;
	dw->WalkCtx.dstLen = GetContentDataMaxLen(dw, btData) - dw->Blk->Len;
	dw->WalkCtx.pdst = dw->Blk->Content.Record.Data + dw->Blk->Len;

	int wr;
	do
	{
		dw->WalkCtx.writed = dw->WalkCtx.readed = 0;
		dw->wqty = dw->PrimSkip;
		wr = WriteSingleValue(dw);

		// Увеличиваем размер занятых данных в текущем блоке на то, что вернул WriteSingleValue.
		// (Если внутри происходил EdfFlushData, 'w' содержит корректный остаток для нового блока)
		if (dw->Blk->Len + dw->WalkCtx.writed > 0xFFFF)
			return ERR_WRONG_PARAMETERS;
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

