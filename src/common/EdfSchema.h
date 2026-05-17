#ifndef SCHEMA_H
#define SCHEMA_H

#include "_pch.h"
#include "EdfStream.h"

typedef struct
{
	uint8_t Count;
	uint16_t* Item;
} EdfDims_t;

typedef struct
{
	uint8_t Count;
	struct EdfType* Item;
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
size_t GetTotalElements(const EdfDims_t* const dims);


uint32_t GetTypeCSize(const EdfType_t* t);
int8_t HasDynamicFields(const EdfType_t* t);
int WriteSchemaBinToStream(Stream_t* st, const EdfSchema_t* t, size_t* writed);
int WriteSchemaTxtToStream(Stream_t* st, const EdfSchema_t* t, size_t* writed);
int WriteSchemaBinToCBin(uint8_t* src, size_t srcLen, size_t* readed,
	uint8_t* dst, size_t dstLen, size_t* writed,
	EdfSchema_t** t);

#endif
