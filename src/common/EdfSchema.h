#ifndef SCHEMA_H
#define SCHEMA_H

#include "_pch.h"
#include "EdfStream.h"

typedef struct
{
	uint8_t Count;
	uint32_t* Item;
} Dims_t;

typedef struct
{
	uint8_t Count;
	struct EdfType* Item;
} Childs_t;

typedef struct EdfType
{
	uint8_t Type; /// PoType 
	char* Name;
	Dims_t Dims;
	Childs_t Childs;
} EdfType_t;

typedef struct
{
	int16_t Id;		// Schema id
	char* Name;		// Schema name
	char* Desc;		// Schema description
	EdfType_t Type; // Schema type
} EdfSchema_t;

int IsVar(const EdfSchema_t* r, int32_t varId, const char* varName);
int IsVarName(const EdfSchema_t* r, const char* varName);
size_t GetTotalElements(const Dims_t* const dims);


uint32_t GetTypeCSize(const EdfType_t* t);
int8_t HasDynamicFields(const EdfType_t* t);
int WriteSchemaBinToStream(Stream_t* st, const EdfSchema_t* t, size_t* writed);
int WriteSchemaTxtToStream(Stream_t* st, const EdfSchema_t* t, size_t* writed);
int WriteSchemaBinToCBin(uint8_t* src, size_t srcLen, size_t* readed,
	uint8_t* dst, size_t dstLen, size_t* writed,
	EdfSchema_t** t);

#endif
