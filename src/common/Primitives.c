#include "_pch.h"
#include "edf.h"
#include "itoa.h"

void putchar_(char character)
{
	// заглушка 
	(void)character;
}

//-----------------------------------------------------------------------------
typedef int (*WriteStringFn)(WalkContext_t*);
//-----------------------------------------------------------------------------
static int WriteStringCBinToStr(WalkContext_t* ctx)
{
	if (IoBufferGetRemain(&ctx->Src) < sizeof(char*))
		return ERR_SRC_SHORT;
	// print text without buf
	const char* str = *(char**)ctx->Src.Pos;
	size_t len = (NULL == str) ? 0 : strnlength(str, MAX_STR_LEN);
	if (IoBufferGetRemain(&ctx->Dst) < len + 2)
		return ERR_DST_SHORT;
	*ctx->Dst.Pos = '"';
	memcpy(ctx->Dst.Pos + 1, str, len);
	*(ctx->Dst.Pos + len + 1) = '"';
	IoBufferAdvance(&ctx->Dst, 2 + len);
	IoBufferAdvance(&ctx->Src, sizeof(char*));
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringBinToStr(WalkContext_t* ctx)
{
	size_t sLen = ctx->Src.Pos[0];
	if (IoBufferGetRemain(&ctx->Src) < 1 + sLen)
		return ERR_SRC_SHORT;
	const char* found = memchr(&ctx->Src.Pos[1], '\0', sLen);
	size_t pos = found - (char*)(&ctx->Src.Pos[1]);
	if (pos < sLen)
		sLen = pos;
	if (IoBufferGetRemain(&ctx->Dst) < sLen + 2)
		return ERR_DST_SHORT;
	*ctx->Dst.Pos = '"';
	memcpy(ctx->Dst.Pos + 1, &ctx->Src.Pos[1], sLen);
	*(ctx->Dst.Pos + sLen + 1) = '"';
	IoBufferAdvance(&ctx->Dst, 2 + sLen);
	IoBufferAdvance(&ctx->Src, 1 + sLen);
	return 0;
}
//-----------------------------------------------------------------------------
static size_t xprint(const uint8_t* buf, size_t bufLen, char* format, ...)
{
	va_list arglist;
	va_start(arglist, format);
	int writed = vsnprintf_((char*)buf, bufLen, format, arglist);
	va_end(arglist);
	if (writed && (size_t)writed == bufLen)
		return writed + 1;
	return writed;
}
//-----------------------------------------------------------------------------
int CBinToBin(const EdfType_t* t, WalkContext_t* ctx)
{
	size_t readed, writed;
	switch (t->Type)
	{
	case Struct:
	default: return ERR_WRONG_TYPE;
	case Int8: case UInt8:
		if (sizeof(uint8_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint8_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*ctx->Dst.Pos = *ctx->Src.Pos;
		writed = readed = sizeof(uint8_t);
		break;
	case Half: case Int16: case UInt16:
		if (sizeof(uint16_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint16_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*(uint16_t*)ctx->Dst.Pos = *(const uint16_t*)ctx->Src.Pos;
		writed = readed = sizeof(uint16_t);
		break;
	case Single: case Int32: case UInt32:
		if (sizeof(uint32_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint32_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*(uint32_t*)ctx->Dst.Pos = *(const uint32_t*)ctx->Src.Pos;
		writed = readed = sizeof(uint32_t);
		break;
	case Double: case Int64: case UInt64:
		if (sizeof(uint64_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint64_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*(uint32_t*)ctx->Dst.Pos = *(const uint32_t*)ctx->Src.Pos;//memcpy(dst, src, *r); break;
		*(uint32_t*)(ctx->Dst.Pos + 4) = *(const uint32_t*)(ctx->Src.Pos + 4);
		writed = readed = sizeof(uint64_t);
		break;
	case Char:
		size_t totalChars = GetTotalElements((EdfDims_t*)&t->Dims);
		if (totalChars > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (totalChars > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		memcpy(ctx->Dst.Pos, ctx->Src.Pos, totalChars);
		writed = readed = totalChars;
		break;
	case String:
		if (sizeof(char*) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		const char* str = *(char**)ctx->Src.Pos;
		size_t len = (NULL == str) ? 0 : strnlength(str, MAX_STR_LEN);
		if (len + 1 > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		ctx->Dst.Pos[0] = (uint8_t)len;
		memcpy(ctx->Dst.Pos + 1, str, len);
		writed = len + 1;
		readed = sizeof(char*);
		break;
	}//switch
	IoBufferAdvance(&ctx->Dst, writed);
	IoBufferAdvance(&ctx->Src, readed);
	return 0;
}
//-----------------------------------------------------------------------------
int BinToBin(const EdfType_t* t, WalkContext_t* ctx)
{
	size_t readed, writed;
	switch (t->Type)
	{
	case Struct:
	default: return ERR_WRONG_TYPE;
	case Int8: case UInt8:
		if (sizeof(uint8_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint8_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*ctx->Dst.Pos = *ctx->Src.Pos;
		writed = readed = sizeof(uint8_t);
		break;
	case Half: case Int16: case UInt16:
		if (sizeof(uint16_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint16_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*(uint16_t*)ctx->Dst.Pos = *(const uint16_t*)ctx->Src.Pos;
		writed = readed = sizeof(uint16_t);
		break;
	case Single: case Int32: case UInt32:
		if (sizeof(uint32_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint32_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*(uint32_t*)ctx->Dst.Pos = *(const uint32_t*)ctx->Src.Pos;
		writed = readed = sizeof(uint32_t);
		break;
	case Double: case Int64: case UInt64:
		if (sizeof(uint64_t) > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (sizeof(uint64_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		*(uint32_t*)ctx->Dst.Pos = *(const uint32_t*)ctx->Src.Pos;//memcpy(dst, src, *r); break;
		*(uint32_t*)(ctx->Dst.Pos + 4) = *(const uint32_t*)(ctx->Src.Pos + 4);
		writed = readed = sizeof(uint64_t);
		break;
	case Char:
		size_t totalChars = GetTotalElements((EdfDims_t*)&t->Dims);
		if (totalChars > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (totalChars > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		memcpy(ctx->Dst.Pos, ctx->Src.Pos, totalChars);
		writed = readed = totalChars;
		break;
	case String:
		size_t blength = 1 + ctx->Src.Pos[0];
		if (blength > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (blength > IoBufferGetRemain(&ctx->Src)) return ERR_DST_SHORT;
		memcpy(ctx->Dst.Pos, ctx->Src.Pos, blength);
		writed = readed = blength;
		break;
	}//switch
	IoBufferAdvance(&ctx->Dst, writed);
	IoBufferAdvance(&ctx->Src, readed);
	return 0;
}
//-----------------------------------------------------------------------------
static int AnyBinToStr(const EdfType_t* t, WalkContext_t* ctx, WriteStringFn WriteString)
{
	size_t readed, writed;
	switch (t->Type)
	{
	case Struct:
	default: return ERR_WRONG_TYPE;
	case Int8:
		if (sizeof(int8_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		writed = Int32ToA((int8_t)ctx->Src.Pos[0], (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(int8_t);
		break;
	case UInt8:
		if (sizeof(uint8_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		writed = UInt32ToA((uint8_t)ctx->Src.Pos[0], (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(uint8_t);
		break;
	case Int16:
		if (sizeof(int16_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		writed = Int32ToA(*((int16_t*)ctx->Src.Pos), (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(int16_t);
		break;
	case UInt16:
		if (sizeof(uint16_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		writed = UInt32ToA(*((uint16_t*)ctx->Src.Pos), (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(uint16_t);
		break;
	case Int32:
		if (sizeof(int32_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		writed = Int32ToA(*((int32_t*)ctx->Src.Pos), (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(int32_t);
		break;
	case UInt32:
		if (sizeof(uint32_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		writed = UInt32ToA(*((uint32_t*)ctx->Src.Pos), (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(uint32_t);
		break;
	case Int64:
	{
		if (sizeof(int64_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		int64_t alignedVal;
		memcpy(&alignedVal, ctx->Src.Pos, sizeof(int64_t));
		writed = Int64ToA(alignedVal, (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(uint64_t);
		break;
	}
	case UInt64:
	{
		if (sizeof(uint64_t) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		uint64_t alignedVal;
		memcpy(&alignedVal, ctx->Src.Pos, sizeof(uint64_t));
		writed = UInt64ToA(alignedVal, (char*)ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst));
		if (0 == writed) return ERR_DST_SHORT;
		readed = sizeof(uint64_t);
		break;
	}
	case Half:
		//ctx->writed = sprintf_s(dst, ctx->dstLen, "%g", *((uint16_t*)src));
		return ERR_WRONG_TYPE;
	case Single:
	{
		if (sizeof(float) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		float alignedVal;
		memcpy(&alignedVal, ctx->Src.Pos, sizeof(float));
		writed = xprint(ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst), "%.9g", alignedVal);
		if (writed > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		readed = sizeof(float);
		break;
	}
	case Double:
	{
		if (sizeof(double) > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		double alignedVal;
		memcpy(&alignedVal, ctx->Src.Pos, sizeof(double));
		writed = xprint(ctx->Dst.Pos, IoBufferGetRemain(&ctx->Dst), "%.17g", alignedVal);
		if (writed > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		readed = sizeof(double);
		break;
	}
	case Char:
		size_t totalChars = GetTotalElements((EdfDims_t*)&t->Dims);
		size_t actualChars = strnlength((const char*)ctx->Src.Pos, totalChars);
		if (actualChars + 2 > IoBufferGetRemain(&ctx->Dst)) return ERR_DST_SHORT;
		if (totalChars > IoBufferGetRemain(&ctx->Src)) return ERR_SRC_SHORT;
		ctx->Dst.Pos[0] = '"';
		memcpy(ctx->Dst.Pos + 1, ctx->Src.Pos, actualChars);
		ctx->Dst.Pos[actualChars + 1] = '"';
		readed = totalChars;
		writed = actualChars + 2;
		break;
	case String: return (*WriteString)(ctx);
		
	}//switch (t)

	IoBufferAdvance(&ctx->Dst, writed);
	IoBufferAdvance(&ctx->Src, readed);
	return ERR_NO;
}
//-----------------------------------------------------------------------------
int CBinToStr(const EdfType_t* t, WalkContext_t* ctx)
{
	return AnyBinToStr(t, ctx, WriteStringCBinToStr);
}
//-----------------------------------------------------------------------------
int BinToStr(const EdfType_t* t, WalkContext_t* ctx)
{
	return AnyBinToStr(t, ctx, WriteStringBinToStr);
}
//-----------------------------------------------------------------------------
int StreamWriteString(Stream_t* s, const char* str, size_t* writed)
{
	int err = 0;
	size_t len = str ? strnlength(str, MAX_STR_LEN) : 0;
	if ((err = StreamWrite(s, writed, &len, 1)) ||
		(err = StreamWrite(s, writed, str, len)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
int StreamReadString(MemStream_t* tsrc, LineAlloc_t* tmem, char** ti)
{
	MemStream_t src = *tsrc;
	LineAlloc_t mem = *tmem;
	int err = 0;
	uint8_t sLen;
	char* pstr = NULL;
	if ((err = StreamRead(&src, NULL, &sLen, 1)))
		return ERR_SRC_SHORT;
	if (sLen)
	{
		if ((err = MemAlloc(&mem, sLen, (void**)&pstr)))
			return ERR_DST_SHORT;
		if ((err = StreamRead(&src, NULL, pstr, sLen)))
			return ERR_SRC_SHORT;
		if ('\0' != pstr[sLen - 1])
		{
			// линейный аллокатор добавит '\0' в конце 
			// т.к. при выделении памяти происходит обнуление выделенной памяти
			// следовательно выделенный 1 элемент будет равен '\0' в конце 
			uint8_t* pStrEnd = NULL;
			if ((err = MemAlloc(&mem, 1, (void**)&pStrEnd)))
				return ERR_DST_SHORT;
			pStrEnd[0] = '\0';// на всякий случай, если аллокатор станет без обнуления
		}
	}
	*ti = pstr;
	memcpy(tsrc, &src, sizeof(MemStream_t));
	*tmem = mem;
	return 0;
}
//-----------------------------------------------------------------------------
