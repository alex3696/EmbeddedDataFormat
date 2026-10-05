#include "_pch.h"
#include "KeyValue.h"

//-----------------------------------------------------------------------------
EdfType_t GetUInt16ValueType()
{
	EdfType_t ret =
	{
		Struct, "UInt16Value", { 0 },
		.Fields =
		{
			.Count = 4,
			.Item = (EdfType_t[])
			{
				{ String, "Name" },
				{ UInt16, "Value" },
				{ String, "Unit" },
				{ String, "Description" },
			}
		}
	};
	return ret;
}
//-----------------------------------------------------------------------------
int UnpackUInt16KeyVal(MemStream_t* src, const EdfType_t* t, LineAlloc_t* dst,
	size_t* skip, DoOnItemUInt16 DoOnItem, void* state)
{
	size_t primReaded = 0;
	int err = 0;
	UInt16Value_t* s = (*skip) ? (UInt16Value_t*)dst->Buffer : NULL;
	while (!(err = EdfReadBin(t, src, dst, (void **)&s, skip, &primReaded)))
	{
		(*DoOnItem)(s, state);
		s = NULL;
		*skip = 0;
		dst->WPos = 0;
	}
	return err;
}

//-----------------------------------------------------------------------------
EdfType_t GetUInt32ValueType()
{
	EdfType_t ret =
	{
		Struct, "UInt32Value", { 0 },
		.Fields =
		{
			.Count = 4,
			.Item = (EdfType_t[])
			{
				{ String, "Name" },
				{ UInt32, "Value" },
				{ String, "Unit" },
				{ String, "Description" },
			}
		}
	};
	return ret;
}
//-----------------------------------------------------------------------------
int UnpackUInt32KeyVal(MemStream_t* src, const EdfType_t* t, LineAlloc_t* dst,
	size_t* skip, DoOnItemUInt32Fn DoOnItem, void* state)
{
	size_t primReaded = 0;
	int err = 0;
	UInt32Value_t* s = (*skip) ? (UInt32Value_t*)dst->Buffer : NULL;
	while (!(err = EdfReadBin(t, src, dst, (void **)&s, skip, &primReaded)))
	{
		(*DoOnItem)(s, state);
		s = NULL;
		*skip = 0;
		dst->WPos = 0;
	}
	return err;
}

//-----------------------------------------------------------------------------
EdfType_t GetDoubleValueType()
{
	EdfType_t ret =
	{
		Struct, "DoubleValue", { 0 },
		.Fields =
		{
			.Count = 4,
			.Item = (EdfType_t[])
			{
				{ String, "Name" },
				{ Double, "Value" },
				{ String, "Unit" },
				{ String, "Description" },
			}
		}
	};
	return ret;
}
//-----------------------------------------------------------------------------
int UnpackDoubleKeyVal(MemStream_t* src, const EdfType_t* t, LineAlloc_t* dst,
	size_t* skip, DoOnItemDoubleFn DoOnItem, void* state)
{
	size_t primReaded = 0;
	int err = 0;
	DoubleValue_t* s = (*skip) ? (DoubleValue_t*)dst->Buffer : NULL;
	while (!(err = EdfReadBin(t, src, dst, (void **)&s, skip, &primReaded)))
	{
		(*DoOnItem)(s, state);
		s = NULL;
		*skip = 0;
		dst->WPos = 0;
	}
	return err;
}
//-----------------------------------------------------------------------------
