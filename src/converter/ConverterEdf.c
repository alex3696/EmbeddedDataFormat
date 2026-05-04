#include "_pch.h"
#include "assert.h"
#include "converter.h"
#include "edf_cfg.h"
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
	EdfWriter_t br = { 0 };
	EdfWriter_t tw = { 0 };
	if (EdfOpen(&br, src, "rb"))
		LOG_ERR();
	if (EdfOpen(&tw, dst, "wtc"))
		LOG_ERR();

	size_t writed = 0;
	int err = 0;

	while (!(err = EdfReadBlock(&br)))
	{
		switch (br.Blk.Type)
		{
		default: break;
		case btHeader:
			if ((err = EdfWriteHeader(&tw, &br.Cfg, &writed)))
				return err;
			break;
		case btVarInfo:
		{
			tw.TypePtr = NULL;
			err = StreamWriteBinToCBin(br.Blk.Data, br.Blk.Len, NULL, br.Buf, sizeof(br.Buf), NULL, &tw.TypePtr);
			if (!err)
			{
				writed = 0;
				err = EdfWriteInfo(&tw, tw.TypePtr, &writed);
			}
			else
			{
				//err = 0;
				//return err;// ignore wrong or too big info block
			}
		}
		break;
		case btVarData:
		{
			EdfWriteDataBlock(&tw, &br.Blk.Data, br.Blk.Len);
			//EdfFlushDataBlock(&tw, &writed);
		}
		break;
		}
		if (0 != err)
		{
			LOG_ERR();
			break;
		}
	}
	EdfClose(&br);
	EdfClose(&tw);
	return 0;
}
//-----------------------------------------------------------------------------
int TextToBin(const char* src, const char* dst)
{
	UNUSED(src);
	UNUSED(dst);
	return 0;
}
