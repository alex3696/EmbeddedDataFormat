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
int BinToText(const char* srcFile, const char* dstFile)
{
	int err = 0;
	size_t writed = 0;
	uint8_t edfMemReader[MEM_BLOCK_SIZE_512];
	const EdfConfig_t cfg = { EDF_VERSMAJOR,EDF_VERSMINOR, EDF_ENCODING, 512, 0, Default };
	EdfContext_t* br = EdfCreate(edfMemReader, sizeof(edfMemReader), &cfg, &err);
	if (err)
		return err;
	uint8_t edfMemWriter[MEM_BLOCK_SIZE_512];
	EdfContext_t* tw = EdfCreate(edfMemWriter, sizeof(edfMemWriter), &cfg, &err);
	if (err)
		return err;

	if (EdfOpenFile(br, srcFile, "rb"))
		LOG_ERR();
	if (EdfOpenFile(tw, dstFile, "wt"))
		LOG_ERR();

	// будем конвертировать из Edf строк сразу в текст без промежуточного конвертирования в Си строки (char*)
	// для этого переопределяем функцию чтения и записи примитивов, в частности для String(char*)
	// поскольку в Си char* может ссылаться в любую область, а в Edf строка BStr-формата без терминатора
	EdfImpl_t impl = *tw->impl;
	impl.WritePrimitive = BinToStr;
	tw->impl = &impl;
	/*
	size_t skip = 0;
	void* dst = NULL;
	MemStream_t mem;
	uint8_t edfMem[4096] = { 0 };
	if ((err = MemStreamWriteOpen(&mem, edfMem, sizeof(edfMem))))
		return err;
	*/

	while (!(err = EdfReadBlock(br)))
	{
		switch (br->Blk->Type)
		{
		default: break;
		case btConfig:
			tw->Cfg = br->Cfg;
			if ((err = EdfWriteConfig(tw, &writed)))
				return err;
			break;
		case btSchema:
		{
			tw->SchemaPtr = NULL;
			err = WriteSchemaBinToCBin(br->Blk->Conent.Schema.Data, GetContentDataLen(br->Blk), NULL, br->Buf, br->Cfg.Blocksize, NULL, &tw->SchemaPtr);
			if (!err)
			{
				writed = 0;
				/*
				skip = 0;
				dst = NULL;
				mem.WPos = 0;
				*/
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
			/*
			MemStream_t src;
			if ((err = MemStreamReadOpen(&src, br->Blk->Conent.Record.Data, GetContentDataLen(br->Blk))))
				return err;
			size_t primReaded = 0;
			do
			{
				primReaded = 0;
				if ((err = EdfReadBin(&tw->SchemaPtr->Type, &src, &mem, &dst, &(size_t){skip}, &primReaded)))
				{
					if (ERR_SRC_SHORT == err)
					{
						err = 0;
						skip += primReaded;
						continue;
					}
					else
						return err;
				}
				else
					skip = 0;
				if ((err = EdfWriteData(tw, dst, GetTypeCSize(&tw->SchemaPtr->Type))))
					return err;
			} while (primReaded && StreamLen(&src));
			*/
			EdfWriteData(tw, br->Blk->Conent.Record.Data, GetContentDataLen(br->Blk));
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
