#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int EdfWriteSep(EdfContext_t* dw,
	const char* const src,
	uint8_t** dst, size_t* dstSize,
	size_t* skip, size_t* wqty,
	size_t* writed)
{
	// игнорируем любые разделители, если мы не в текстовом режиме
	if(dw->WritePrimitive == CBinToBin)
		return 0;

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
	{
		int err = 0;
		dw->Blk->Len += (uint16_t)(*writed);
		if ((err = EdfFlushData(dw, writed)))
			return err;
		*writed = 0;
		*dstSize = GetContentDataMaxLen(dw, btData);
		*dst = dw->Blk->Content.Record.Data;
		if (srcLen > *dstSize)
			return ERR_DST_SHORT;
	}
	(*wqty)++;
	memcpy(*dst, src, srcLen);
	(*dstSize) -= srcLen;
	(*writed) += srcLen;
	(*dst) += srcLen;
	return 0;
}

#define EdfWriteSepBeginStruct(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepBeginStruct, dst, dstLen, skip, wqty, writed))
#define EdfWriteSepEndStruct(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepEndStruct, dst, dstLen, skip, wqty, writed))
#define EdfWriteSepBeginArray(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepBeginArray, dst, dstLen, skip, wqty, writed))
#define EdfWriteSepEndArray(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepEndArray, dst, dstLen, skip, wqty, writed))
#define EdfWriteSepRecBegin(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepRecBegin, dst, dstLen, skip, wqty, writed))
#define EdfWriteSepRecEnd(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepRecEnd, dst, dstLen, skip, wqty, writed))
#define EdfWriteSepVarEnd(ctx, dst, dstLen, skip, wqty, writed) (EdfWriteSep(ctx, SepVarEnd, dst, dstLen, skip, wqty, writed))

//-----------------------------------------------------------------------------
// 
static int WriteOnePrimitive(EdfContext_t* dw, const EdfType_t* t,
	const uint8_t** ppsrc, size_t* srcLen,
	uint8_t** ppdst, size_t* dstLen,
	size_t* skip, size_t* wqty,
	size_t* readed, size_t* writed)
{
	if (0 < (*skip))
	{
		(*skip)--;
		return ERR_NO;
	}
	int err = 0;
	size_t r = 0, w = 0;
	size_t charLen;
	if (Char == t->Type)
	{
		charLen = GetTotalElements(&t->Dims);
		if (charLen == 0)
			return ERR_WRONG_TYPE;
		if (charLen > *srcLen)
			return ERR_SRC_SHORT;
	}
	else
	{
		charLen = *srcLen;
	}
	if ((err = (*dw->WritePrimitive)(t->Type, *ppsrc, charLen, *ppdst, *dstLen, &r, &w)))
	{
		if (ERR_DST_SHORT != err)
			return err;
		// Сбрасываем блок
		dw->Blk->Len += (uint16_t)(*writed);
		dw->PrimSkip = (uint16_t)(*wqty);
		if ((err = EdfFlushData(dw, &w)))
			return err;
		// Сбрасываем счетчики для нового блока
		*writed = 0;
		*dstLen = GetContentDataMaxLen(dw, btData);
		*ppdst = dw->Blk->Content.Record.Data;
		// Пытаемся записать ЕЩЕ РАЗ
		if ((err = (*dw->WritePrimitive)(t->Type, *ppsrc, charLen, *ppdst, *dstLen, &r, &w)))
			return err;// если снова ошибка, выходим
	}
	(*wqty)++;
	*readed += r;
	*writed += w;
	*ppsrc += r; *srcLen -= r;
	*ppdst += w; *dstLen -= w;
	return err;
}
//-----------------------------------------------------------------------------
static int WriteElement(const EdfType_t* t,
	const uint8_t** ppsrc, size_t *srcLen,
	uint8_t** ppdst, size_t *dstLen,
	size_t* skip, size_t* wqty,
	size_t* readed, size_t* writed,
	EdfContext_t* dw)
{
	int err = ERR_NO;
	if (Char == t->Type)
	{
		if ((err = WriteOnePrimitive(dw, t, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed)))
			return err;
#ifndef EDF_DISABLE_TEXT_MODE
		return EdfWriteSepVarEnd(dw, ppdst, dstLen, skip, wqty, writed);
#else
		return err;
#endif
	}
	size_t totalElement = GetTotalElements(&t->Dims);
#ifndef EDF_DISABLE_TEXT_MODE
	if (1 < totalElement)
	{
		if ((err = EdfWriteSepBeginArray(dw, ppdst, dstLen, skip, wqty, writed)))
			return err;
	}
#endif
	for (size_t i = 0; i < totalElement; i++)
	{
		if (Struct == t->Type)
		{
			if (t->Fields.Count)
			{
#ifndef EDF_DISABLE_TEXT_MODE
				if ((err = EdfWriteSepBeginStruct(dw, ppdst, dstLen, skip, wqty, writed)))
					return err;
#endif
				for (size_t j = 0; j < t->Fields.Count; j++)
				{
					const EdfType_t* s = &t->Fields.Item[j];
					if ((err = WriteElement(s, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed, dw)))
						return err;
				}
#ifndef EDF_DISABLE_TEXT_MODE
				if ((err = EdfWriteSepEndStruct(dw, ppdst, dstLen, skip, wqty, writed)))
					return err;
#endif
			}
		}
		else
		{
			if ((err = WriteOnePrimitive(dw, t, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed)))
				return err;
#ifndef EDF_DISABLE_TEXT_MODE
			if ((err = (EdfWriteSepVarEnd(dw, ppdst, dstLen, skip, wqty, writed))))
				return err;
#endif
		}
	}
#ifndef EDF_DISABLE_TEXT_MODE
	if (1 < totalElement)
	{
		if ((err = (EdfWriteSepEndArray(dw, ppdst, dstLen, skip, wqty, writed))))
			return err;
	}
#endif
	return err;
}
//-----------------------------------------------------------------------------
static int WriteSingleValue(EdfContext_t* dw,
	const uint8_t** src, size_t* srcLen,
	uint8_t** dst, size_t* dstLen,
	size_t* skip, size_t* wqty,
	size_t* readed, size_t* writed)
{
	int err;
#ifndef EDF_DISABLE_TEXT_MODE
	if (ERR_NO != (err = EdfWriteSepRecBegin(dw, dst, dstLen, skip, wqty, writed)))
		return err;
#endif
	if (ERR_NO != (err = WriteElement(&dw->SchemaPtr->Type, src, srcLen, dst, dstLen, skip, wqty, readed, writed, dw)))
		return err;
#ifndef EDF_DISABLE_TEXT_MODE
	if (ERR_NO != (err = EdfWriteSepRecEnd(dw, dst, dstLen, skip, wqty, writed)))
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

	const uint8_t* src = (const uint8_t*)vsrc;
	size_t srcLen = xsrcLen;

	size_t dstLen = GetContentDataMaxLen(dw, btData) - dw->Blk->Len;
	uint8_t* dst = dw->Blk->Content.Record.Data + dw->Blk->Len;

	int wr;
	do
	{
		size_t skip = dw->PrimSkip;
		size_t r = 0, w = 0, wqty = dw->PrimSkip;
		wr = WriteSingleValue(dw, &src, &srcLen, &dst, &dstLen, &skip, &wqty, &r, &w);
		// Увеличиваем размер занятых данных в текущем блоке на то, что вернул WriteSingleValue.
		// (Если внутри происходил EdfFlushData, 'w' содержит корректный остаток для нового блока)
		if (dw->Blk->Len + w > 0xFFFF)
			return ERR_WRONG_PARAMETERS;
		dw->Blk->Len += (uint16_t)w;
		if (srcConsumed != NULL)
			*srcConsumed += r;

		switch (wr)
		{
		default:
		case ERR_WRONG_TYPE: return ERR_WRONG_TYPE;
		case ERR_SRC_SHORT:
			// Входной буфер оборвался на середине примитива/массива.
			// Запоминаем позицию в схеме (wqty), чтобы при следующем вызове начать с нужного места.
			dw->PrimSkip = (uint16_t)wqty;
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
	} while (0 < srcLen);
	return wr;
}

