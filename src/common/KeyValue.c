#include "_pch.h"
#include "KeyValue.h"

//-----------------------------------------------------------------------------
int UnpackUInt16KeyVal(MemStream_t* src, MemStream_t* dst,
	size_t* skip, DoOnItemUInt16 DoOnItem, void* state)
{
	size_t primReaded = 0;
	int err = 0;
	UInt16Value_t* s = (*skip) ? (UInt16Value_t*)dst->Buffer : NULL;
	while (!(err = EdfReadBin(&UInt16ValueType, src, dst, (void **)&s, skip, &primReaded)))
	{
		(*DoOnItem)(s, state);
		s = NULL;
		*skip = 0;
		dst->WPos = 0;
	}
	return err;
}
//-----------------------------------------------------------------------------
int UnpackUInt32KeyVal(MemStream_t* src, MemStream_t* dst,
	size_t* skip, DoOnItemUInt32Fn DoOnItem, void* state)
{
	size_t primReaded = 0;
	int err = 0;
	UInt32Value_t* s = (*skip) ? (UInt32Value_t*)dst->Buffer : NULL;
	while (!(err = EdfReadBin(&UInt32ValueType, src, dst, (void **)&s, skip, &primReaded)))
	{
		(*DoOnItem)(s, state);
		s = NULL;
		*skip = 0;
		dst->WPos = 0;
	}
	return err;
}
//-----------------------------------------------------------------------------
int UnpackDoubleKeyVal(MemStream_t* src, MemStream_t* dst,
	size_t* skip, DoOnItemDoubleFn DoOnItem, void* state)
{
	size_t primReaded = 0;
	int err = 0;
	DoubleValue_t* s = (*skip) ? (DoubleValue_t*)dst->Buffer : NULL;
	while (!(err = EdfReadBin(&DoubleValueType, src, dst, (void **)&s, skip, &primReaded)))
	{
		(*DoOnItem)(s, state);
		s = NULL;
		*skip = 0;
		dst->WPos = 0;
	}
	return err;
}
//-----------------------------------------------------------------------------
