#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
// 
static int WriteOnePrimitive(EdfWriter_t* dw, const EdfType_t* t,
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
		dw->Blk.Len += (uint16_t)(*writed);
		if ((err = EdfFlushData(dw, &w)))
			return err;
		*writed = 0;
		*dstLen = sizeof(dw->Blk.Data);
		*ppdst = dw->Blk.Data;
		if ((err = (*dw->WritePrimitive)(t->Type, *ppsrc, charLen, *ppdst, *dstLen, &r, &w)))
			return err;
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
	EdfWriter_t* dw)
{
	int err = ERR_NO;
	if (Char == t->Type)
	{
		if ((err = WriteOnePrimitive(dw, t, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed)))
			return err;
		return EdfWriteSep(dw->SepVarEnd, ppdst, dstLen, skip, wqty, writed);
	}
	size_t totalElement = GetTotalElements(&t->Dims);
	if (1 < totalElement)
	{
		if ((err = EdfWriteSep(dw->BeginArray, ppdst, dstLen, skip, wqty, writed)))
			return err;
	}
	for (size_t i = 0; i < totalElement; i++)
	{
		if (Struct == t->Type)
		{
			if (t->Childs.Count)
			{
				if ((err = EdfWriteSep(dw->BeginStruct, ppdst, dstLen, skip, wqty, writed)))
					return err;
				for (size_t j = 0; j < t->Childs.Count; j++)
				{
					const EdfType_t* s = &t->Childs.Item[j];
					if ((err = WriteElement(s, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed, dw)))
						return err;
				}
				if ((err = EdfWriteSep(dw->EndStruct, ppdst, dstLen, skip, wqty, writed)))
					return err;
			}
		}
		else
		{
			if ((err = WriteOnePrimitive(dw, t, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed)))
				return err;
			if ((err = (EdfWriteSep(dw->SepVarEnd, ppdst, dstLen, skip, wqty, writed))))
				return err;
		}
	}
	if (1 < totalElement)
	{
		if ((err = (EdfWriteSep(dw->EndArray, ppdst, dstLen, skip, wqty, writed))))
			return err;
	}
	return err;
}
//-----------------------------------------------------------------------------
static int WriteSingleValue(EdfWriter_t* dw,
	const uint8_t** src, size_t* srcLen,
	uint8_t** dst, size_t* dstLen,
	size_t* skip, size_t* wqty,
	size_t* readed, size_t* writed)
{
	int err;
	if (ERR_NO != (err = EdfWriteSep(dw->RecBegin, dst, dstLen, skip, wqty, writed)))
		return err;
	if (ERR_NO != (err = WriteElement(&dw->TypePtr->Inf, src, srcLen, dst, dstLen, skip, wqty, readed, writed, dw)))
		return err;
	if (ERR_NO != (err = EdfWriteSep(dw->RecEnd, dst, dstLen, skip, wqty, writed)))
		return err;
	return err;
}
//-----------------------------------------------------------------------------
int EdfWriteData(EdfWriter_t* dw, const void* vsrc, size_t xsrcLen)
{
	const uint8_t* xsrc = (const uint8_t*)vsrc;
	const uint8_t* src = xsrc;
	size_t srcLen = xsrcLen;

	size_t dstLen = sizeof(dw->Blk.Data) - dw->Blk.Len;
	uint8_t* dst = dw->Blk.Data + dw->Blk.Len;

	int wr;
	do
	{
		if (dw->BufLen)
		{
			// copy xsrc data to buffer
			size_t len = MIN(sizeof(dw->Buf) - dw->BufLen, xsrcLen);
			if (0 < len)
			{
				memcpy(dw->Buf + dw->BufLen, xsrc, len);
				xsrc += len;
				xsrcLen -= len;
				dw->BufLen += len;

				src = dw->Buf;
				srcLen = dw->BufLen;
			}
		}
		else
		{
			src = xsrc;
			srcLen = xsrcLen;
		}

		size_t skip = dw->Skip;
		size_t r = 0, w = 0, wqty = 0;
		wr = WriteSingleValue(dw, &src, &srcLen, &dst, &dstLen, &skip, &wqty, &r, &w);

		if (dw->BufLen)
		{
			dw->BufLen -= r;
			if (dw->BufLen)
			{
				memcpy(dw->Buf, src + r, dw->BufLen);
				src = dw->Buf;
				srcLen = dw->BufLen;
			}
			else
			{
				src = xsrc;
				srcLen = xsrcLen;
			}
			if (0 > wr && 0 < xsrcLen)
				wr = 0;
		}
		else
		{
			xsrc += r;
			xsrcLen -= r;
			src = xsrc;
			srcLen = xsrcLen;
		}

		dw->Blk.Len += (uint16_t)w;
		switch (wr)
		{
		default:
		case ERR_WRONG_TYPE: return ERR_WRONG_TYPE;
		case ERR_SRC_SHORT:
			dw->Skip += wqty;
			break;
		case ERR_NO:
			dw->Skip = 0;
			if (0 == xsrcLen)
				return ERR_NO;
			break;
		case ERR_DST_SHORT:
			if ((wr == EdfFlushData(dw, &w)))
				return wr;
			dstLen = sizeof(dw->Blk.Data);
			dst = dw->Blk.Data;
			dw->Skip += wqty;
			wr = 0;
			break;
		}
	} while (ERR_SRC_SHORT != wr && 0 < srcLen);

	if (ERR_SRC_SHORT == wr && 0 < srcLen)
	{
		dw->BufLen = srcLen;
		memcpy(dw->Buf, src, dw->BufLen);
	}
	return wr;
}

