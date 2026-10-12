#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int EdfWriteSep(EdfContext_t* edf, const char* const sep)
{
	// игнорируем любые разделители, если мы не в текстовом режиме
	if (edf->WritePrimitive == CBinToBin)
		return 0;

	if (0 < edf->PrimSkip)
	{
		edf->PrimSkip--;
		return 0;
	}
	size_t sepLen = sep ? strnlength(sep, 10) : 0;
	if (!sepLen)
	{
		edf->CurrPrim++;
		return 0;
	}
	if (sepLen > IoBufferGetRemain(&edf->WalkCtx.Dst))
	{
		int err = 0;
		//edf->Blk->Len = (uint16_t)IoBufferGetAdvanced(&edf->WalkCtx.Dst);
		if ((err = EdfFlushData(edf, NULL)))
			return err;
		IoBufferReset(&edf->WalkCtx.Dst);
	}
	memcpy(edf->WalkCtx.Dst.Pos, sep, sepLen);
	IoBufferAdvance(&edf->WalkCtx.Dst, sepLen);
	edf->CurrPrim++;
	edf->Blk->Len = (uint16_t)IoBufferGetAdvanced(&edf->WalkCtx.Dst);
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
	if ((err = (edf->WritePrimitive)(t, &edf->WalkCtx)))
	{
		if (ERR_DST_SHORT != err)
			return err;
		// Сбрасываем блок
		//edf->Blk->Len = (uint16_t)IoBufferGetAdvanced(&edf->WalkCtx.Dst);
		edf->PrimSkip = edf->CurrPrim;
		if ((err = EdfFlushData(edf, NULL)))
			return err;
		IoBufferReset(&edf->WalkCtx.Dst);
		edf->PrimSkip = 0;// Сбрасываем счетчики для нового блока
		if ((err = (edf->WritePrimitive)(t, &edf->WalkCtx)))// Пытаемся записать ЕЩЕ РАЗ
			return err;// если снова ошибка, выходим
	}
	edf->CurrPrim++;
	edf->Blk->Len = (uint16_t)IoBufferGetAdvanced(&edf->WalkCtx.Dst);
	return err;
}
//-----------------------------------------------------------------------------
#ifdef EDF_ENABLE_TEXT_MODE
static int WriteElement(const EdfType_t* t, EdfContext_t* edf)
{
	int err = ERR_NO;
	if (Char == t->Type)
	{
		if ((err = WriteOnePrimitive(t, edf)))
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
#endif
static int WriteElement_BinMode(const EdfType_t* t, EdfContext_t* edf)
{
	int err = ERR_NO;
	if (Struct == t->Type)
	{
		size_t totalElement = GetTotalElements((EdfDims_t*)&t->Dims);
		while (totalElement--)
		{
			for (size_t j = 0; j < t->Fields.Count; j++)
			{
				if ((err = WriteElement_BinMode(&t->Fields.Item[j], edf)))
					return err;
			}
		}
	}
	else
	{
		if (Char == t->Type)
			return WriteOnePrimitive(t, edf);
		size_t totalElement = GetTotalElements((EdfDims_t*)&t->Dims);
		if (0 < edf->PrimSkip)
		{
			if (totalElement < edf->PrimSkip)
			{
				edf->PrimSkip -= (uint16_t)totalElement;
				totalElement = 0;
			}
			else
			{
				totalElement -= edf->PrimSkip;
				edf->PrimSkip = 0;
			}
		}
		while (totalElement--)
		{
			if ((err = WriteOnePrimitive(t, edf)))
				return err;
		}
	}
	return ERR_NO;
}
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
	IoBufferInit(&dw->WalkCtx.Src, (uint8_t*)vsrc, xsrcLen, 0);
	IoBufferInit(&dw->WalkCtx.Dst, dw->Blk->Content.Record.Data, GetDataMaxLen(dw->Cfg.Blocksize), dw->Blk->Len);
	int wr;
	do
	{
		dw->CurrPrim = dw->PrimSkip;
#ifdef EDF_ENABLE_TEXT_MODE
		if (dw->WritePrimitive == CBinToBin)
			wr = WriteElement_BinMode(&dw->SchemaPtr->Type, dw);
		else
			wr = WriteSingleValue(dw);
#else
		wr = WriteElement_BinMode(&dw->SchemaPtr->Type, dw);
#endif
		if (srcConsumed != NULL)
			*srcConsumed = IoBufferGetAdvanced(&dw->WalkCtx.Src);

		switch (wr)
		{
		default:
		case ERR_WRONG_TYPE: return ERR_WRONG_TYPE;
		case ERR_SRC_SHORT:
			// Входной буфер оборвался на середине примитива/массива.
			// Запоминаем позицию в схеме (CurrPrim), чтобы при следующем вызове начать с нужного места.
			dw->PrimSkip = dw->CurrPrim;
			return ERR_SRC_SHORT;
		case ERR_NO:
			dw->PrimSkip = 0;
			dw->RecordId++;
			break;
		case ERR_DST_SHORT:
			//dstLen = GetContentDataMaxLen(dw, btData);
			//dst = dw->Blk->Content.Record.Data;
			//dw->PrimSkip = (uint16_t)CurrPrim;
			//wr = 0;
			return ERR_DST_SHORT;
			break;
		}
	} while (0 < IoBufferGetRemain(&dw->WalkCtx.Src));
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
		size_t structsCount = xsrcLen / structLen;
		size_t rem = xsrcLen % structLen;
		while (structsCount)
		{
			size_t maxFullStructsCount = (GetDataMaxLen(dw->Cfg.Blocksize) - dw->Blk->Len) / structLen;
			size_t currCount = structsCount < maxFullStructsCount ? structsCount : maxFullStructsCount;
			if (!currCount) // если полностью в блок структура не входит
			{
				if ((dw->Cfg.Flags & DisableStructBlockTransfer)
					|| (GetDataMaxLen(dw->Cfg.Blocksize) == dw->Blk->Len))
				{
					if ((err = EdfFlushData(dw, NULL)))
						return err;
					continue;
				}
				else
				{
					if ((err = EdfWriteDataParse(dw, (void*)src, structLen, srcConsumed)))
						return err;
					structsCount -= 1;
					src += structLen;
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
			structsCount -= currCount;
			src += currLen;
		}
		if (rem)
			return EdfWriteDataParse(dw, src, rem, srcConsumed);
	}
	else
		return EdfWriteDataParse(dw, vsrc, xsrcLen, srcConsumed);
	return ERR_NO;
}
