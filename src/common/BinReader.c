#include "_pch.h"
#include "edf.h"

//-----------------------------------------------------------------------------
static int ReadPrimitive(const TypeInfo_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
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
static int ReadStruct(const TypeInfo_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
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

	for (size_t j = 0; j < t->Childs.Count; j++)
	{
		const TypeInfo_t* s = &t->Childs.Item[j];
		size_t childCLen = GetTypeCSize(s);
		if ((err = EdfReadBin(s, src, mem, (void**)&ti, resultPrimOffset, primReaded)))
			return err;
		ti += childCLen;
	}
	return err;
}
//-----------------------------------------------------------------------------
static int ReadElement(const TypeInfo_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
	if (Struct == t->Type)
		return ReadStruct(t, src, mem, presult, resultPrimOffset, primReaded);
	return ReadPrimitive(t, src, mem, presult, resultPrimOffset, primReaded);
}
//-----------------------------------------------------------------------------
static int ReadArray(const TypeInfo_t* t, MemStream_t* src, size_t totalElement, MemStream_t* mem, void** presult,
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
int EdfReadBin(const TypeInfo_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded)
{
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

	dw->BlkType = 0;
	dw->DatLen = 0;
	// read Block Type
	if ((err = StreamRead(&dw->Stream, &readed, &dw->BlkType, 1)))
		return err;
	if (!IsBlockType(dw->BlkType))
		return ERR_BLK_WRONG_TYPE;
	// read Block Sequence
	uint8_t blockseq;
	if ((err = StreamRead(&dw->Stream, &readed, &blockseq, 1)))
		return err;
	if (blockseq != dw->BlkSeq)
		return ERR_BLK_WRONG_SEQ;
	// read Block Length
	if ((err = StreamRead(&dw->Stream, &readed, &dw->DatLen, 2)))
		return err;
	if (4096 < dw->DatLen || BLOCK_SIZE < dw->DatLen)
		return ERR_BLK_WRONG_SIZE;
	// read Block Content
	if ((err = StreamRead(&dw->Stream, &readed, &dw->Block, dw->DatLen)))
		return err;
	// read Block CRC
	uint16_t crcFile = 0;
	if ((err = StreamRead(&dw->Stream, &readed, &crcFile, sizeof(uint16_t))))
		return err;
	// calculate Block CRC
	uint16_t crcData = MbCrc16(&dw->BlkType, 4 + dw->DatLen);
	if (crcData != crcFile)
		return ERR_BLK_WRONG_CRC;

	// try read cfg
	if (btHeader == dw->BlkType)
	{
		if ((err = MakeHeaderFromBytes(dw->Block, dw->DatLen, &dw->Cfg)))
			return err;
		if (dw->Cfg.Blocksize < BLOCK_SIZE)
			return ERR_BLOCK_SIZE_LARGE;
		dw->BlkSeq = 0;
	}

	dw->BlkSeq++;
	return 0;

}
