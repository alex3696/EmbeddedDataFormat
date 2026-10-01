#include "_pch.h"
#include "edf.h"
#include "itoa.h"
//-----------------------------------------------------------------------------
static void MemStreamMove(MemStream_t* s)
{
	if (s->RPos)
	{
		s->WPos -= s->RPos;//dataLen
		memcpy(s->Buffer, &s->Buffer[s->RPos], s->WPos);
		s->RPos = 0;
	}
}
//-----------------------------------------------------------------------------
static int MemStreamWriteImpl(void* stream, size_t* writed, void const* data, size_t len)
{
	MemStream_t* s = (MemStream_t*)stream;
	size_t rempty = s->RPos;
	size_t wempty = s->Size - s->WPos;
	if (len > rempty + wempty)
		return ERR_DST_SHORT;
	if (len > wempty)
		MemStreamMove(s);
	memcpy(&s->Buffer[s->WPos], data, len);
	s->WPos += len;
	if (writed)
		*writed += len;
	return 0;
}
//-----------------------------------------------------------------------------
static int MemStreamReadImpl(void* stream, size_t* readed, void* dst, size_t len)
{
	MemStream_t* s = (MemStream_t*)stream;
	if (StreamLen(stream) < len)
		return ERR_SRC_SHORT;
	memcpy(dst, &s->Buffer[s->RPos], len);
	s->RPos += len;
	if (readed)
		*readed += len;
	return 0;
}
//-----------------------------------------------------------------------------
static int MemStreamClose(void* stream)
{
	MemStream_t* s = (MemStream_t*)stream;
	memset(s, 0, sizeof(MemStream_t));
	return 0;
}
//-----------------------------------------------------------------------------
size_t StreamLen(const MemStream_t* s)
{
	return s->WPos - s->RPos;
}
//-----------------------------------------------------------------------------
size_t StreamEmptyLen(const MemStream_t* s)
{
	return s->Size - (s->WPos - s->RPos);
}
//-----------------------------------------------------------------------------
int StreamCpy(MemStream_t* src, MemStream_t* dst, size_t len)
{
	if (StreamLen(src) < len)
		return ERR_SRC_SHORT;
	if (StreamEmptyLen(dst) < len)
		return ERR_DST_SHORT;
	MemStreamMove(dst);
	memcpy(&dst->Buffer[dst->WPos], &src->Buffer[src->RPos], len);
	dst->WPos += len;
	src->RPos += len;
	return 0;
}
//-----------------------------------------------------------------------------
int MemStreamReadOpen(MemStream_t* s, uint8_t* buf, size_t size)
{
	return MemStreamOpen(s, buf, size, size, "r");
}
//-----------------------------------------------------------------------------
int MemStreamWriteOpen(MemStream_t* s, uint8_t* buf, size_t size)
{
	return MemStreamOpen(s, buf, size, 0, "w");
}

const StreamFnImpl_t rwMemSt = { T_MEM_STREAM, MemStreamWriteImpl, MemStreamReadImpl, MemStreamClose };
const StreamFnImpl_t wMemSt =  { T_MEM_STREAM, MemStreamWriteImpl, NULL,              MemStreamClose };
const StreamFnImpl_t rMemSt =  { T_MEM_STREAM, NULL,               MemStreamReadImpl, MemStreamClose };

//-----------------------------------------------------------------------------
int MemStreamOpen(MemStream_t* s, uint8_t* buf, size_t size, size_t datalen, const char* inMode)
{
	if (NULL == inMode || 0 == strcmp("rw", inMode) || 0 == strcmp("wr", inMode))
	{
		s->Impl = &rwMemSt;
		s->Buffer = buf;
		s->Size = size;
		s->RPos = 0;
		s->WPos = datalen;
		return 0;
	}
	else if (0 == strcmp("w", inMode) || 0 == strcmp("wb", inMode))
	{
		s->Impl = &wMemSt;
		s->Buffer = buf;
		s->Size = size;
		s->RPos = 0;
		s->WPos = 0;
		return 0;
	}
	else if (0 == strcmp("r", inMode) || 0 == strcmp("rb", inMode))
	{
		s->Impl = &rMemSt;
		s->Buffer = buf;
		s->Size = size;
		s->RPos = 0;
		s->WPos = size;
		return 0;
	}
	return ERR_WRONG_PARAMETERS;
}
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//Memory Linear Allocator
int LineAllocInit(LineAlloc_t* w, uint8_t* buf, size_t size)
{
	w->Buffer = buf;
	w->Size = size;
	w->WPos = 0;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
size_t MemGetAvailableLen(LineAlloc_t* w)
{
	return w->Size - w->WPos;
}
//-----------------------------------------------------------------------------
int MemAlloc(LineAlloc_t* m, size_t len, void** pptr)
{
	if (0 == len)
	{
		*pptr = NULL;
		return ERR_NO;
	}
	if (MemGetAvailableLen(m) < len)
		return ERR_DST_SHORT;
	*pptr = &m->Buffer[m->WPos];
	memset(&m->Buffer[m->WPos], 0, len);
	m->WPos += len;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
#ifdef EDF_ENABLE_TEXT_MODE
static int MemStreamWriteUInt32Txt(MemStream_t* s, size_t* writed, uint32_t val)
{
	MemStreamMove(s);
	size_t bufFreeLen = s->Size - s->WPos;
	if (0 == bufFreeLen)
		return ERR_DST_SHORT;
	size_t ret = UInt32ToA(val, (char*)&s->Buffer[s->WPos], bufFreeLen);
	if (0 == ret)
		return ERR_DST_SHORT;
	s->WPos += ret;
	if (writed)
		(*writed) += ret;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
static int FileStreamWriteBufUInt32Txt(FileStream_t* st, size_t* writed, uint32_t val, uint8_t* buf, size_t bufLen)
{
	size_t ret = UInt32ToA(val, (char*)buf, bufLen);
	if (0 == ret)
		return ERR_DST_SHORT;
	return StreamWrite(st, writed, buf, ret);
}
//-----------------------------------------------------------------------------
static int FileStreamWriteUInt32Txt(FileStream_t* st, size_t* writed, uint32_t val)
{
	return FileStreamWriteBufUInt32Txt((FileStream_t*)st, writed, val, st->FmtBuf, sizeof(st->FmtBuf));
	//if (st->FmtBuf)
	//{
	//	return FileStreamWriteBufUInt32Txt((FileStream_t*)st, writed, val, st->FmtBuf, sizeof(st->FmtBuf));
	//}
	//else
	//{
	//	uint8_t buf[24];
	//	return FileStreamWriteBufUInt32Txt((FileStream_t*)st, writed, val, buf, sizeof(buf));
	//}
}
#endif
//-----------------------------------------------------------------------------
int StreamWriteUInt32Txt(Stream_t* st, size_t* writed, uint32_t val)
{
#ifdef EDF_ENABLE_TEXT_MODE
	switch (st->Impl->TypeId)
	{
	default: break;
	case T_MEM_STREAM: return MemStreamWriteUInt32Txt((MemStream_t*)st, writed, val);
	case T_FILE_STREAM: return FileStreamWriteUInt32Txt((FileStream_t*)st, writed, val);
	}
#endif
	(void)st;
	(void)writed;
	(void)val;
	return ERR_WRONG_TYPE;
}
