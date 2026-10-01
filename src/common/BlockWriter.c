#include "_pch.h"
#include "edf.h"

typedef struct
{
	const uint8_t* psrc;
	uint8_t* pdst;
	size_t srcLen;
	size_t dstLen;
	size_t readed;
	size_t writed;

	EdfContext_t* edf;
	size_t skip;
	size_t wqty;
} WalkContext_t;

//-----------------------------------------------------------------------------
static int EdfWriteSep(WalkContext_t* ctx, const char* const sep)
{
	// игнорируем любые разделители, если мы не в текстовом режиме
	if(ctx->edf->WritePrimitive == CBinToBin)
		return 0;

	if (0 < ctx->skip)
	{
		ctx->skip--;
		return 0;
	}
	size_t sepLen = sep ? strnlength(sep, 10) : 0;
	if (!sepLen)
	{
		ctx->wqty++;
		return 0;
	}
	if (sepLen > ctx->dstLen)
	{
		int err = 0;
		ctx->edf->Blk->Len += (uint16_t)(ctx->writed);
		if ((err = EdfFlushData(ctx->edf, &ctx->writed)))
			return err;
		ctx->writed = 0;
		ctx->dstLen = GetContentDataMaxLen(ctx->edf, btData);
		ctx->pdst = ctx->edf->Blk->Content.Record.Data;
		if (sepLen > ctx->dstLen)
			return ERR_DST_SHORT;
	}
	ctx->wqty++;
	memcpy(ctx->pdst, sep, sepLen);
	ctx->dstLen -= sepLen;
	ctx->writed += sepLen;
	ctx->pdst += sepLen;
	return 0;
}

#define EdfWriteSepBeginStruct(ctx) (EdfWriteSep(ctx, SepBeginStruct))
#define EdfWriteSepEndStruct(ctx) (EdfWriteSep(ctx, SepEndStruct))
#define EdfWriteSepBeginArray(ctx) (EdfWriteSep(ctx, SepBeginArray))
#define EdfWriteSepEndArray(ctx) (EdfWriteSep(ctx, SepEndArray))
#define EdfWriteSepRecBegin(ctx) (EdfWriteSep(ctx, SepRecBegin))
#define EdfWriteSepRecEnd(ctx) (EdfWriteSep(ctx, SepRecEnd))
#define EdfWriteSepVarEnd(ctx) (EdfWriteSep(ctx, SepVarEnd))

//-----------------------------------------------------------------------------
// 
static int WriteOnePrimitive(const EdfType_t* t, WalkContext_t* ctx)
{
	if (0 < (ctx->skip))
	{
		ctx->skip--;
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
		if (charLen > ctx->srcLen)
			return ERR_SRC_SHORT;
	}
	else
	{
		charLen = ctx->srcLen;
	}
	if ((err = (ctx->edf->WritePrimitive)(t->Type, ctx->psrc, charLen, ctx->pdst, ctx->dstLen, &r, &w)))
	{
		if (ERR_DST_SHORT != err)
			return err;
		// Сбрасываем блок
		ctx->edf->Blk->Len += (uint16_t)(ctx->writed);
		ctx->edf->PrimSkip = (uint16_t)(ctx->wqty);
		if ((err = EdfFlushData(ctx->edf, &w)))
			return err;
		// Сбрасываем счетчики для нового блока
		ctx->writed = 0;
		ctx->dstLen = GetContentDataMaxLen(ctx->edf, btData);
		ctx->pdst = ctx->edf->Blk->Content.Record.Data;
		// Пытаемся записать ЕЩЕ РАЗ
		if ((err = (ctx->edf->WritePrimitive)(t->Type, ctx->psrc, charLen, ctx->pdst, ctx->dstLen, &r, &w)))
			return err;// если снова ошибка, выходим
	}
	(ctx->wqty)++;
	ctx->readed += r;
	ctx->writed += w;
	ctx->psrc += r;
	ctx->srcLen -= r;
	ctx->pdst += w;
	ctx->dstLen -= w;
	return err;
}
//-----------------------------------------------------------------------------
static int WriteElement(const EdfType_t* t, WalkContext_t* ctx)
{
	int err = ERR_NO;
	if (Char == t->Type)
	{
		if ((err = WriteOnePrimitive(t, ctx)))
			return err;
#ifdef EDF_ENABLE_TEXT_MODE
		return EdfWriteSepVarEnd(ctx);
#else
		return err;
#endif
	}
	size_t totalElement = GetTotalElements(&t->Dims);
#ifdef EDF_ENABLE_TEXT_MODE
	if (1 < totalElement)
	{
		if ((err = EdfWriteSepBeginArray(ctx)))
			return err;
	}
#endif
	for (size_t i = 0; i < totalElement; i++)
	{
		if (Struct == t->Type)
		{
#ifdef EDF_ENABLE_TEXT_MODE
			if ((err = EdfWriteSepBeginStruct(ctx)))
				return err;
#endif
			for (size_t j = 0; j < t->Fields.Count; j++)
			{
				const EdfType_t* s = &t->Fields.Item[j];
				if ((err = WriteElement(s, ctx)))
					return err;
			}
#ifdef EDF_ENABLE_TEXT_MODE
			if ((err = EdfWriteSepEndStruct(ctx)))
				return err;
#endif
		}
		else
		{
			if ((err = WriteOnePrimitive(t, ctx)))
				return err;
#ifdef EDF_ENABLE_TEXT_MODE
			if ((err = (EdfWriteSepVarEnd(ctx))))
				return err;
#endif
		}
	}
#ifdef EDF_ENABLE_TEXT_MODE
	if (1 < totalElement)
	{
		if ((err = (EdfWriteSepEndArray(ctx))))
			return err;
	}
#endif
	return err;
}
//-----------------------------------------------------------------------------
static int WriteSingleValue(WalkContext_t* ctx)
{
	int err;
#ifdef EDF_ENABLE_TEXT_MODE
	if (ERR_NO != (err = EdfWriteSepRecBegin(ctx)))
		return err;
#endif
	if (ERR_NO != (err = WriteElement(&ctx->edf->SchemaPtr->Type, ctx)))
		return err;
#ifdef EDF_ENABLE_TEXT_MODE
	if (ERR_NO != (err = EdfWriteSepRecEnd(ctx)))
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

	WalkContext_t ctx = { 0 };
	ctx.edf = dw;
	ctx.psrc = (const uint8_t*)vsrc;
	ctx.srcLen = xsrcLen;
	ctx.dstLen = GetContentDataMaxLen(dw, btData) - dw->Blk->Len;
	ctx.pdst = dw->Blk->Content.Record.Data + dw->Blk->Len;

	int wr;
	do
	{
		ctx.writed = ctx.readed = 0;
		ctx.wqty = ctx.skip = dw->PrimSkip;
		wr = WriteSingleValue(&ctx);

		// Увеличиваем размер занятых данных в текущем блоке на то, что вернул WriteSingleValue.
		// (Если внутри происходил EdfFlushData, 'w' содержит корректный остаток для нового блока)
		if (dw->Blk->Len + ctx.writed > 0xFFFF)
			return ERR_WRONG_PARAMETERS;
		dw->Blk->Len += (uint16_t)ctx.writed;
		if (srcConsumed != NULL)
			*srcConsumed += ctx.readed;

		switch (wr)
		{
		default:
		case ERR_WRONG_TYPE: return ERR_WRONG_TYPE;
		case ERR_SRC_SHORT:
			// Входной буфер оборвался на середине примитива/массива.
			// Запоминаем позицию в схеме (wqty), чтобы при следующем вызове начать с нужного места.
			dw->PrimSkip = (uint16_t)ctx.wqty;
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
	} while (0 < ctx.srcLen);
	return wr;
}

