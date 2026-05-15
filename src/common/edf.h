#ifndef EDF_H
#define EDF_H
//-----------------------------------------------------------------------------
#include "edf_cfg.h"
//-----------------------------------------------------------------------------
#define EDF_VERSMAJOR	0		
#define EDF_VERSMINOR	3
#define EDF_ENCODING	65001	//UTF-8

#define MIN_BLOCK_SIZE	256
#define MAX_BLOCK_SIZE	4096

#define MEM_BLOCK_SIZE_256	(sizeof(EdfWriter_t) + MIN_BLOCK_SIZE*2)
#define MEM_BLOCK_SIZE_512	(sizeof(EdfWriter_t) + 512*2)
#define MEM_BLOCK_SIZE_1024	(sizeof(EdfWriter_t) + 1024*2)
#define MEM_BLOCK_SIZE_2084	(sizeof(EdfWriter_t) + 2048*2)
#define MEM_BLOCK_SIZE_4096	(sizeof(EdfWriter_t) + MAX_BLOCK_SIZE*2)

#define MAX_STR_LEN		255

#define EDF_CONSTSTR(val) &(const char*){val}

#define ERR_EOF -1
#define ERR_NO 0

// error codes do not overlap with errno

#define ERR_BASE 1000
#define ERR_SRC_SHORT			ERR_BASE+1
#define ERR_DST_SHORT			ERR_BASE+2
#define ERR_WRONG_TYPE			ERR_BASE+3
#define ERR_WRONG_PARAMETERS	ERR_BASE+4
#define ERR_FREAD				ERR_BASE+5
#define ERR_FWRITE				ERR_BASE+6
#define ERR_FN_NOT_EXIST		ERR_BASE+7

#define ERR_BLK ERR_BASE+100
#define ERR_BLK_WRONG_TYPE		ERR_BLK+20
#define ERR_BLK_WRONG_SIZE		ERR_BLK+21
#define ERR_BLK_WRONG_CRC		ERR_BLK+22
#define ERR_BLOCK_SIZE_LARGE    ERR_BLK+23
//-----------------------------------------------------------------------------
#ifdef __cplusplus
extern "C" {
#endif
//-----------------------------------------------------------------------------
#pragma pack(push,1)
#include "EdfWriter.h"
#pragma pack(pop)
//-----------------------------------------------------------------------------
// инициализация и создание экземпляра EdfWriter_t необходимого для
// хранения конфигурациоонных и промежуточных данных.
int EdfInit(EdfWriter_t* pEdf, uint8_t* pMem, size_t memLen, const EdfConfig_t* pCfg);
EdfWriter_t* EdfCreate(uint8_t* pMem, size_t memLen, const EdfConfig_t* pCfg, int* pErr);
 
// Открыть поток для чтения (до)записи, поток может быть файловым или память
// Режимы: "wb"/"rb" — бинарные, "wt"/"rt" — текстовые, "ab"/"at" — дозапись
int EdfOpenStream(EdfWriter_t* w, Stream_t* stream, const char* mode);
// Открыть файл для чтения (до)записи, внутри обращается к EdfOpenStream
int EdfOpenWithFs(EdfWriter_t* w, const char* file, const char* mode, FileStreamOpenFn fnOpen);
int EdfOpenFile(EdfWriter_t* w, const char* file, const char* mode);
// освобождает фнутренние буферы и закрывает файли или поток, 
int EdfClose(EdfWriter_t* dw);
// запись конфигурации
int EdfWriteConfig(EdfWriter_t* dw, size_t* writed);
// запись схемы данных
int EdfWriteSchema(EdfWriter_t* dw, const EdfSchema_t* t, size_t* writed);
// запись данных
int EdfWriteData(EdfWriter_t* dw, const void* src, size_t srcLen);
// закрывает и скидывает текущий блок на диск  
int EdfFlushData(EdfWriter_t* dw, size_t* writed);
// Чтение данных используя схему 
int EdfReadBin(const EdfType_t* t, MemStream_t* src, MemStream_t* mem, void** presult,
	size_t* resultPrimOffset, size_t* primReaded);
// чтение блока
int EdfReadBlock(EdfWriter_t* dr);

//shortcut: запись схемы + данных
int EdfWriteSchemaData(EdfWriter_t* dw, const EdfSchema_t* ir, const void* d, size_t len);
int EdfWritePrimSchData(EdfWriter_t* dw, PoType pt, uint16_t schId, char* schName, char* schDesc, const void* d);
//-----------------------------------------------------------------------------
#ifdef __cplusplus
}
#endif
//-----------------------------------------------------------------------------
#endif //EDF_H
