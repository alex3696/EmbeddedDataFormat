#ifndef BLOCKWRITER_H
#define BLOCKWRITER_H

#include "EdfStream.h"
#include "EdfConfig.h"
#include "EdfSchema.h"
#include "Primitives.h"

typedef struct EdfWriter EdfWriter_t;

typedef int (*FlushDataFn)(EdfWriter_t* w, size_t* writed);
typedef int (*WriteConfigFn)(EdfWriter_t* w, const EdfConfig_t* h, size_t* writed);
typedef int (*WriteSchemaFn)(EdfWriter_t* w, const EdfSchema_t* t, size_t* writed);

//-----------------------------------------------------------------------------
typedef struct
{
	//uint16_t SchId;			// Id - идентификатор СХЕМЫ
	uint8_t Data[MAX_BLOCK_SIZE - 3 - 2];
} EdfSchemaContent_t;

typedef struct
{
	uint16_t SchId;			// SchemaId - идентификатор схемы (0-65535)
	uint16_t PrmOffset;		// PrimitiveOffset - смещение примитива от начала ЗАПИСИ внутри ЗАПИСИ(0-65535)
	uint32_t RecId;			// RecordId - номер ЗАПИСИ с которой начинается блок 
	uint8_t Data[MAX_BLOCK_SIZE - 3 - 8 - 2];
} EdfRecordContent_t;

typedef struct
{
	uint8_t Type;
	uint16_t Len;
	union
	{
		uint8_t Raw[MAX_BLOCK_SIZE - 3 - 2];
		EdfConfig_t Config;
		EdfSchemaContent_t Schema;
		EdfRecordContent_t Record;
	} Conent;
	//uint16_t Crc;
} EdfBlock_t;

typedef struct EdfImpl
{
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
} EdfImpl_t;
//typedef struct EdfImpl EdfImpl_t;

uint16_t GetContentLen(const EdfBlock_t* blk);

//-----------------------------------------------------------------------------
typedef struct EdfWriter
{
	EdfConfig_t Cfg;
	const EdfSchema_t* SchemaPtr;
	Stream_t Stream;

	const size_t RecMaxLen;
	const size_t SchMaxLen;
	EdfBlock_t* const Blk;

	const size_t BufMaxLen;
	size_t BufLen;
	uint8_t* const Buf;

	const EdfImpl_t* impl;

} EdfWriter_t;


//-----------------------------------------------------------------------------
#endif
