#ifndef BLOCKWRITER_H
#define BLOCKWRITER_H

#include "EdfStream.h"
#include "EdfConfig.h"
#include "EdfSchema.h"
#include "Primitives.h"

typedef struct EdfContext EdfContext_t;

typedef int (*FlushDataFn)(EdfContext_t* w, size_t* writed);
typedef int (*WriteConfigFn)(EdfContext_t* w, const EdfConfig_t* h, size_t* writed);
typedef int (*WriteSchemaFn)(EdfContext_t* w, const EdfSchema_t* t, size_t* writed);

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

uint16_t GetContentMaxLen(const EdfContext_t* pEdf);
uint16_t GetContentDataMaxLen(const EdfContext_t* pEdf, EdfBlockType bt);
uint16_t GetContentDataLen(const EdfBlock_t* blk);

//-----------------------------------------------------------------------------
typedef struct EdfContext
{
	EdfConfig_t Cfg;				// конфигурация
	const EdfSchema_t* SchemaPtr;	// текущая схема, при записи кешируем схему в Buf
	Stream_t Stream;				// поток в который пишем или читаем

	uint16_t PrimSkip;	/** <Смещение примитива внутри текущей записи (0-65535).
							Используется при разрыве примитива между блоками.
                            Сбрасывается в 0 при вызове EdfWriteSchema.> */
	uint32_t RecordId;	/** <Номер текущей записи (счетчик успешно завершенных записей).
							Инкрементируется после каждой полной записи.
							Сбрасывается в 0 при вызове EdfWriteSchema. */

	EdfBlock_t* const Blk;	// буфер блока

	size_t BufLen;
	uint8_t* const Buf;

	const EdfImpl_t* impl;

} EdfContext_t;


//-----------------------------------------------------------------------------
#endif
