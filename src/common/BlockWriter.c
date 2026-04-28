#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
// 
int EdfWritePrimitive(EdfWriter_t* dw, PoType pot,
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
	if ((err = (*dw->WritePrimitive)(pot, *ppsrc, *srcLen, *ppdst, *dstLen, &r, &w)))
	{
		if (ERR_DST_SHORT != err)
			return err;
		dw->DatLen += (uint16_t)(*writed);
		if ((err = EdfFlushDataBlock(dw, &w)))
			return err;
		*writed = 0;
		*dstLen = sizeof(dw->Block);
		*ppdst = dw->Block;
		if ((err = (*dw->WritePrimitive)(pot, *ppsrc, *srcLen, *ppdst, *dstLen, &r, &w)))
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
static int WriteData(const TypeInfo_t* t,
	const uint8_t** ppsrc, size_t *srcLen,
	uint8_t** ppdst, size_t *dstLen,
	size_t* skip, size_t* wqty,
	size_t* readed, size_t* writed,
	EdfWriter_t* dw)
{
	int err = 0;
	size_t totalElement = GetTotalElements(&t->Dims);
	if (Char == t->Type)
	{
		if (*srcLen < totalElement)
			return ERR_SRC_SHORT;
		if (*dstLen < totalElement)
			return ERR_DST_SHORT;
		if (totalElement <= *skip)
			*skip -= totalElement;
		size_t charLen = totalElement;
		if ((err = EdfWritePrimitive(dw, t->Type, ppsrc, &charLen, ppdst, dstLen, skip, wqty, readed, writed)))
			return err;
		if ((err = (EdfWriteSep(dw->SepVarEnd, ppdst, dstLen, skip, wqty, writed))))
			return err;
		return 0;
	}
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
					const TypeInfo_t* s = &t->Childs.Item[j];
					if ((err = WriteData(s, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed, dw)))
						return err;
				}
				if ((err = EdfWriteSep(dw->EndStruct, ppdst, dstLen, skip, wqty, writed)))
					return err;
			}
		}
		else
		{
			if ((err = EdfWritePrimitive(dw, t->Type, ppsrc, srcLen, ppdst, dstLen, skip, wqty, readed, writed)))
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
	if (ERR_NO != (err = WriteData(&dw->t->Inf, src, srcLen, dst, dstLen, skip, wqty, readed, writed, dw)))
		return err;
	if (ERR_NO != (err = EdfWriteSep(dw->RecEnd, dst, dstLen, skip, wqty, writed)))
		return err;
	return err;
}
//-----------------------------------------------------------------------------
int EdfWriteDataBlock(EdfWriter_t* dw, const void* vsrc, size_t xsrcLen)
{
	const uint8_t* xsrc = (const uint8_t*)vsrc;
	const uint8_t* src = xsrc;
	size_t srcLen = xsrcLen;

	size_t dstLen = sizeof(dw->Block) - dw->DatLen;
	uint8_t* dst = dw->Block + dw->DatLen;

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

		dw->DatLen += (uint16_t)w;
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
			if ((wr == EdfFlushDataBlock(dw, &w)))
				return wr;
			dstLen = sizeof(dw->Block);
			dst = dw->Block;
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
//-----------------------------------------------------------------------------
int EdfReadBin(const TypeInfo_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	int* skip)
{
	if (!IsPoType(t->Type))
		return ERR_WRONG_TYPE;

	size_t itemCLen = GetTypeCSize(t);
	int err = 0;
	uint8_t* ti = NULL;
	if (*presult)
		ti = *presult;
	else
	{
		if ((err = MemAlloc(mem, itemCLen, (void**)&ti)))
			return err;
		*presult = ti;
	}

	switch (t->Type)
	{
	case Struct:
		if (t->Childs.Count)
		{
			for (size_t j = 0; j < t->Childs.Count; j++)
			{
				const TypeInfo_t* s = &t->Childs.Item[j];
				size_t childCLen = GetTypeCSize(s);
				if ((err = EdfReadBin(s, src, mem, (void**)&ti, skip)))
					return err;
				ti += childCLen;
			}
		}
		break;
	case String:
	{
		if (0 <= ++(*skip))
			if ((err = StreamReadString(src, mem, (char**)ti)))
				return err;
		ti += itemCLen; // проверить
	}
	break;
	default:
		if (0 <= ++(*skip))
			if ((err = StreamRead(src, NULL, ti, itemCLen)))
				return ERR_SRC_SHORT;
		ti += itemCLen; // проверить
		break;
	}//switch
	return 0;
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
int EdfReadBlock(EdfWriter_t* dw)
{
	int err = 0;
	size_t readed = 0;

	dw->BlkType = 0;
	dw->DatLen = 0;

	if ((err = StreamRead(&dw->Stream, &readed, &dw->BlkType, 1)))
		return err;
	if (!IsBlockType(dw->BlkType))
		return ERR_BLK_WRONG_TYPE;

	uint8_t blockseq;
	if ((err = StreamRead(&dw->Stream, &readed, &blockseq, 1)))
		return err;
	if (blockseq != dw->BlkSeq)
		return ERR_BLK_WRONG_SEQ;

	if ((err = StreamRead(&dw->Stream, &readed, &dw->DatLen, 2)))
		return err;
	if (4096 < dw->DatLen || BLOCK_SIZE < dw->DatLen)
		return ERR_BLK_WRONG_SIZE;

	if ((err = StreamRead(&dw->Stream, &readed, &dw->Block, dw->DatLen)))
		return err;

	if (btHeader == dw->BlkType)
		memcpy(&dw->h, &dw->Block, sizeof(EdfHeader_t));

	uint16_t crcData = MbCrc16(&dw->BlkType, 4 + dw->DatLen);
	uint16_t crcFile = 0;
	if ((err = StreamRead(&dw->Stream, &readed, &crcFile, sizeof(uint16_t))))
		return err;
	if (crcData != crcFile)
		return ERR_BLK_WRONG_CRC;

	dw->BlkSeq++;
	return 0;

}
