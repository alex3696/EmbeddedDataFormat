#ifndef KEYVALUE_H
#define KEYVALUE_H

#include "edf.h"
//-----------------------------------------------------------------------------
#pragma pack(push,1)
//-----------------------------------------------------------------------------
EdfType_t GetUInt16ValueType();
typedef struct UInt16Value
{
	char* Name;
	uint16_t Value;
	char* Unit;
	char* Description;
} UInt16Value_t;

typedef void (*DoOnItemUInt16)(UInt16Value_t* s, void* state);

int UnpackUInt16KeyVal(MemStream_t* src, const EdfType_t* t, LineAlloc_t* dst,
	size_t* skip, DoOnItemUInt16 DoOnItem, void* state);
//-----------------------------------------------------------------------------
EdfType_t GetUInt32ValueType();
typedef struct UInt32Value
{
	char* Name;
	uint32_t Value;
	char* Unit;
	char* Description;
} UInt32Value_t;

typedef void (*DoOnItemUInt32Fn)(UInt32Value_t* s, void* state);

int UnpackUInt32KeyVal(MemStream_t* src, const EdfType_t* t, LineAlloc_t* dst,
	size_t* skip, DoOnItemUInt32Fn DoOnItem, void* state);
//-----------------------------------------------------------------------------
EdfType_t GetDoubleValueType();
typedef struct DoubleValue
{
	char* Name;
	double Value;
	char* Unit;
	char* Description;
} DoubleValue_t;

typedef void (*DoOnItemDoubleFn)(DoubleValue_t* s, void* state);
int UnpackDoubleKeyVal(MemStream_t* src, const EdfType_t* t, LineAlloc_t* dst,
	size_t* skip, DoOnItemDoubleFn DoOnItem, void* state);
//-----------------------------------------------------------------------------
#pragma pack(pop)
//-----------------------------------------------------------------------------
#endif
