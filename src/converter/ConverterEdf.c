#include "_pch.h"
#include "assert.h"
#include "converter.h"
#include "edf.h"
#include "math.h"
//-----------------------------------------------------------------------------
static char* GetFileExt(const char* filename) {
	char* dot = strrchr(filename, '.');
	if (!dot || dot == filename) return "";
	return dot + 1;
}
//-----------------------------------------------------------------------------
int IsExt(const char* file, const char* ext)
{
	size_t extLen = strlen(ext);
	size_t fileLen = strlen(file);
	const char* fileExt = file + fileLen - extLen;
	return 0 == _strcmpi(fileExt, ext);
}
//-----------------------------------------------------------------------------
int ChangeExt(char* file, const char* ext)
{
	// remove ext
	char* fileExt = GetFileExt(file);
	if (fileExt && 0 != strlen(fileExt))
	{
		*fileExt = '\0';
	}
	// add ext
	size_t fileLen = strlen(file);
	memcpy(file + fileLen, ext, strlen(ext) + 1);
	return 0;
}
//-----------------------------------------------------------------------------
int BinToText(const char* src, const char* dst)
{
	int err = 0;
	uint8_t edfMemReader[DEFAULT_MEM_BLOCK_SIZE] = { 0 };
	EdfWriter_t* br = EdfCreate(edfMemReader, sizeof(edfMemReader), NULL, &err);
	if (err)
		return err;
	uint8_t edfMemWriter[DEFAULT_MEM_BLOCK_SIZE] = { 0 };
	EdfWriter_t* tw = EdfCreate(edfMemWriter, sizeof(edfMemWriter), NULL, &err);
	if (err)
		return err;

	if (EdfOpenFile(br, src, "rb"))
		LOG_ERR();
	if (EdfOpenFile(tw, dst, "wtc"))
		LOG_ERR();

	size_t writed = 0;
	

	while (!(err = EdfReadBlock(br)))
	{
		switch (br->Blk->Type)
		{
		default: break;
		case btConfig:
			if ((err = EdfWriteConfig(tw, &br->Cfg, &writed)))
				return err;
			break;
		case btSchema:
		{
			tw->SchemaPtr = NULL;
			err = WriteSchemaBinToCBin(br->Blk->Conent.Schema.Data, GetContentLen(br->Blk), NULL, br->Buf, br->BufMaxLen, NULL, &tw->SchemaPtr);
			if (!err)
			{
				writed = 0;
				err = EdfWriteSchema(tw, tw->SchemaPtr, &writed);
			}
			else
			{
				//err = 0;
				//return err;// ignore wrong or too big Schema block
			}
		}
		break;
		case btData:
		{
			EdfWriteData(tw, br->Blk->Conent.Record.Data, GetContentLen(br->Blk));
			//EdfFlushData(&tw, &writed);
		}
		break;
		}
		if (0 != err)
		{
			LOG_ERR();
			break;
		}
	}
	EdfClose(br);
	EdfClose(tw);
	return 0;
}
//-----------------------------------------------------------------------------
int TextToBin(const char* src, const char* dst)
{
	UNUSED(src);
	UNUSED(dst);
	return 0;
}
