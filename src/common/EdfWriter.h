#ifndef BLOCKWRITER_H
#define BLOCKWRITER_H

#include "_pch.h"
#include "EdfConfig.h"
#include "EdfStream.h"
#include "Primitives.h"
#include "EdfSchema.h"

typedef struct EdfWriter EdfWriter_t;

typedef int (*FlushDataFn)(EdfWriter_t* w, size_t* writed);
typedef int (*WriteConfigFn)(EdfWriter_t* w, const EdfConfig_t* h, size_t* writed);
typedef int (*WriteSchemaFn)(EdfWriter_t* w, const EdfSchema_t* t, size_t* writed);

int EdfWriteSep(const char* const src,
	uint8_t** dst, size_t* dstSize,
	size_t* skip, size_t* wqty,
	size_t* writed);
//-----------------------------------------------------------------------------
typedef struct
{
	uint8_t Type;
	uint16_t Len;
	uint8_t Data[BLOCK_SIZE];
	uint16_t Crc;
} EdfBlock_t;

typedef struct
{
	uint32_t RecId;			// RecordId - номер ЗАПИСИ с которой начинается блок 
	uint16_t SchId;			// SchemaId - идентификатор схемы (0-65535) 
	uint16_t PrmOffset;		// PrimitiveOffset - смещение примитива от начала ЗАПИСИ внутри ЗАПИСИ(0-65535)
} EdfDataHdr_t;

typedef struct EdfWriter
{
	EdfConfig_t Cfg;
	const EdfSchema_t* SchemaPtr;
	Stream_t Stream;

	uint32_t RecordId;
	size_t Skip;

	EdfBlock_t Blk;

	size_t BufLen;
	uint8_t Buf[BLOCK_SIZE];

	WritePrimitivesFn WritePrimitive;
	WriteConfigFn WriteConfig;
	WriteSchemaFn WriteSchema;
	FlushDataFn FlushData;

	const char* BeginStruct;
	const char* EndStruct;
	const char* BeginArray;
	const char* EndArray;
	const char* SepVarEnd;
	const char* RecBegin;
	const char* RecEnd;
} EdfWriter_t;


//-----------------------------------------------------------------------------
#endif
