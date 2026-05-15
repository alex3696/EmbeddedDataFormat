#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
uint16_t GetContentLen(const EdfBlock_t* blk)
{
	uint16_t len = 0;
	switch (blk->Type)
	{
		default: break;
		case btConfig:
		case btSchema:
			len = blk->Len;
			break;
		case btData:
			len = blk->Len - offsetof(EdfRecordContent_t, Data);
			break;
	}
	return len;
}
//-----------------------------------------------------------------------------
static int ReadPrimitive(const EdfType_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
	if (0 < (*resultPrimOffset))
	{
		(*resultPrimOffset)--;
		return ERR_NO;
	}
	int err = ERR_NO;

	switch (t->Type)
	{
		case String:
			if ((err = StreamReadString(src, mem, (char**)(*presult))))
				return err;
			if (primReaded)
				(*primReaded)++;
			//((uint8_t*)(*presult)) += sizeof(char*);
			break;
		case Char:
		{
			// Char - это массив фиксированной длины
			size_t charArrayLen = GetTotalElements(&t->Dims);
			if (charArrayLen == 0)
				return ERR_WRONG_TYPE;
			if ((err = StreamRead(src, NULL, (uint8_t*)(*presult), charArrayLen)))
				return err;
			if (primReaded)
				(*primReaded)++;
			break;
		}
		default:
		{
			size_t itemCLen = GetSizeOf(t->Type);
			if ((err = StreamRead(src, NULL, (uint8_t*)(*presult), itemCLen)))
				return err;
			if (primReaded)
				(*primReaded)++;
			//((uint8_t*)(*presult)) += itemCLen;
		}
		break;
	}
	return err;
}
//-----------------------------------------------------------------------------
static int ReadStruct(const EdfType_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
	int err = 0;
	uint8_t* ti = *presult;

	for (size_t j = 0; j < t->Fields.Count; j++)
	{
		const EdfType_t* s = &t->Fields.Item[j];
		if ((err = EdfReadBin(s, src, mem, &ti, resultPrimOffset, primReaded)))
			return err;
		size_t childCLen = GetTypeCSize(s);
		ti += childCLen;
	}
	return err;
}
//-----------------------------------------------------------------------------
static int ReadElement(const EdfType_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
	int err = 0;
	// alloc mem
	size_t c_items_len = GetTypeCSize(t);
	uint8_t* ti = NULL;
	if (*presult)
		ti = *presult;
	else
	{
		if ((err = MemAlloc(mem, c_items_len, (void**)&ti)))
			return err;
		*presult = ti;
	}
	if (Struct == t->Type)
		return ReadStruct(t, src, mem, (void**)&ti, resultPrimOffset, primReaded);
	return ReadPrimitive(t, src, mem, (void**)&ti, resultPrimOffset, primReaded);
}
//-----------------------------------------------------------------------------
static int ReadArray(const EdfType_t* t, MemStream_t* src, size_t totalElement, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
	int err = 0;
	// alloc mem
	size_t c_items_len = GetTypeCSize(t);
	uint8_t* ti = NULL;
	if (*presult)
		ti = *presult;
	else
	{
		if ((err = MemAlloc(mem, c_items_len, (void**)&ti)))
			return err;
		*presult = ti;
	}

	size_t c_item_len = c_items_len / totalElement;
	for (size_t i = 0; i < totalElement; i++)
	{
		if((err=ReadElement(t, src, mem, (void**)&ti, resultPrimOffset, primReaded)))
			return err;
		ti += c_item_len;
	}
	return err;
}
//-----------------------------------------------------------------------------
int EdfReadBin(const EdfType_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
	if (t->Type == Char)
		return ReadElement(t, src, mem, presult, resultPrimOffset, primReaded);
	size_t totalElement = GetTotalElements(&t->Dims);
	if (1 < totalElement)
		return ReadArray(t, src, totalElement, mem, presult, resultPrimOffset, primReaded);
	return ReadElement(t, src, mem, presult, resultPrimOffset, primReaded);
}
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
int EdfReadBlock(EdfWriter_t* dw)
{
	int err = 0;
	size_t readed = 0;

	dw->Blk->Type = 0;
	dw->Blk->Len = 0;
	// read Block Type
	if ((err = StreamRead(&dw->Stream, &readed, &dw->Blk->Type, 1)))
		return err;
	if (!IsBlockType(dw->Blk->Type))
		return ERR_BLK_WRONG_TYPE;
	// read Block Length
	if ((err = StreamRead(&dw->Stream, &readed, &dw->Blk->Len, 2)))
		return err;
	if (dw->Cfg.Blocksize < dw->Blk->Len)
		return ERR_BLK_WRONG_SIZE;
	// read Block Content
	if ((err = StreamRead(&dw->Stream, &readed, &dw->Blk->Conent.Schema, dw->Blk->Len)))
		return err;
	// read Block CRC
	uint16_t crcFile = 0;
	if ((err = StreamRead(&dw->Stream, &readed, &crcFile, sizeof(uint16_t))))
		return err;
	// calculate Block CRC
	uint16_t crcData = MbCrc16(&dw->Blk->Type, 3 + dw->Blk->Len);
	if (crcData != crcFile)
		return ERR_BLK_WRONG_CRC;

	// try read cfg
	if (btConfig == dw->Blk->Type)
	{
		if(dw->Cfg.Blocksize < dw->Blk->Conent.Config.Blocksize)
			return ERR_BLOCK_SIZE_LARGE;
		dw->Cfg = dw->Blk->Conent.Config;
	}
	return 0;
}
