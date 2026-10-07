#ifndef SCHEMA_H
#define SCHEMA_H

#include "_pch.h"
#include "EdfStream.h"

typedef struct
{
	uint16_t* Item;
	uint8_t Count;
	uint16_t TotalElements; // кэш итогового количества
} EdfDims_t;

typedef struct
{
	struct EdfType* Item;
	uint8_t Count;
} EdfField_t;

typedef struct EdfType
{
	uint8_t Type; /// PoType 
	char* Name;
	EdfDims_t Dims;
	EdfField_t Fields;
} EdfType_t;

typedef struct
{
	uint16_t Id;	// Schema id
	char* Name;		// Schema name
	char* Desc;		// Schema description
	EdfType_t Type; // Schema type
} EdfSchema_t;

int IsVar(const EdfSchema_t* r, int32_t varId, const char* varName);
int IsVarName(const EdfSchema_t* r, const char* varName);
uint16_t GetTotalElements(EdfDims_t* const dims);

size_t GetEdfSchemaCBinLen(const EdfSchema_t* sch);
size_t GetEdfTypeCBinLen(const EdfType_t* t);
size_t GetTypeCSize(const EdfType_t* t);
int8_t HasDynamicFields(const EdfType_t* t);
int GetTypeInfo(const EdfType_t* t, uint16_t* cSize, uint8_t* hasDynamicFields);
int WriteSchemaBinToStream(Stream_t* st, const EdfSchema_t* t, size_t* writed);
int WriteSchemaTxtToStream(Stream_t* st, const EdfSchema_t* t, size_t* writed);
int WriteSchemaBinToCBin(uint8_t* src, size_t srcLen, size_t* readed,
	uint8_t* dst, size_t dstLen, size_t* writed,
	EdfSchema_t** t);
int SchemaCopyСBinToCBin(const EdfSchema_t* const srcSch, EdfSchema_t** pDstSch,
	uint8_t* dstBuf, size_t dstLen, size_t* writed);

#endif
