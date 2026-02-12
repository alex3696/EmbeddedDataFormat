#ifndef BLOCKWRITER_H
#define BLOCKWRITER_H

#include "_pch.h"
#include "EdfHeader.h"
#include "EdfStream.h"
#include "Primitives.h"
#include "TypeInfo.h"

typedef struct EdfWriter EdfWriter_t;

typedef int (*FlushDataFn)(EdfWriter_t* w, size_t* writed);
typedef int (*WriteHeaderFn)(EdfWriter_t* w, const EdfHeader_t* h, size_t* writed);
typedef int (*WriteInfoFn)(EdfWriter_t* w, const TypeRec_t* t, size_t* writed);

int EdfWriteSep(const char* const src,
	uint8_t** dst, size_t* dstSize,
	size_t* skip, size_t* wqty,
	size_t* writed);
//-----------------------------------------------------------------------------
/*
typedef struct EdfBlock
{
	uint8_t Type;
	uint8_t Seq;
	uint16_t Len;
	uint8_t Data[BLOCK_SIZE];
	uint16_t Crc;
};
*/

typedef struct EdfWriter
{
	EdfHeader_t h;
	const TypeRec_t* t;
	//uint8_t TypeFlag;
	//uint16_t TypeLen;
	Stream_t Stream;
	size_t Skip;

	uint8_t BlkType;
	uint8_t BlkSeq;
	uint16_t DatLen;
	uint8_t Block[BLOCK_SIZE];

	size_t BufLen;
	uint8_t Buf[BLOCK_SIZE];

	WritePrimitivesFn WritePrimitive;
	WriteHeaderFn WriteHeader;
	WriteInfoFn WriteInfo;
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