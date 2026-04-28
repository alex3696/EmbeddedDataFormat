#ifndef TYPEINFO_H
#define TYPEINFO_H

#include "_pch.h"
#include "PoType.h"
#include "EdfStream.h"

typedef struct
{
	uint8_t Count;
	uint32_t* Item;
} Dims_t;

typedef struct
{
	uint8_t Count;
	struct TypeInfo* Item;
} Childs_t;

typedef struct TypeInfo
{
	uint8_t Type; /// PoType 
	char* Name;
	Dims_t Dims;
	Childs_t Childs;
	//uint16_t TypeId; // UserTypeId
} TypeInfo_t;

typedef struct TypeRec
{
	TypeInfo_t Inf; // var type
	int32_t Id; // var id
	char* Name; // var name
	char* Desc; // var description
} TypeRec_t;

int IsVar(const TypeRec_t* r, int32_t varId, const char* varName);
int IsVarName(const TypeRec_t* r, const char* varName);
size_t GetTotalElements(Dims_t* dims);


uint32_t GetTypeCSize(const TypeInfo_t* t);
int8_t HasDynamicFields(const TypeInfo_t* t);
int StreamWriteInfBin(Stream_t* st, const TypeRec_t* t, size_t* writed);
int StreamWriteInfTxt(Stream_t* st, const TypeRec_t* t, size_t* writed);
int StreamWriteBinToCBin(uint8_t* src, size_t srcLen, size_t* readed,
	uint8_t* dst, size_t dstLen, size_t* writed,
	TypeRec_t** t);

#endif
