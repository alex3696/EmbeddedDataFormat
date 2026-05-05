#ifndef TYPEINFO_H
#define TYPEINFO_H

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
	int32_t Id; // var id
	char* Name; // var name
	char* Desc; // var description
	EdfType_t Type; // var type
} EdfInf_t;

int IsVar(const EdfInf_t* r, int32_t varId, const char* varName);
int IsVarName(const EdfInf_t* r, const char* varName);
size_t GetTotalElements(const Dims_t* const dims);


uint32_t GetTypeCSize(const EdfType_t* t);
int8_t HasDynamicFields(const EdfType_t* t);
int StreamWriteInfBin(Stream_t* st, const EdfInf_t* t, size_t* writed);
int StreamWriteInfTxt(Stream_t* st, const EdfInf_t* t, size_t* writed);
int StreamWriteBinToCBin(uint8_t* src, size_t srcLen, size_t* readed,
	uint8_t* dst, size_t dstLen, size_t* writed,
	EdfInf_t** t);

#endif
