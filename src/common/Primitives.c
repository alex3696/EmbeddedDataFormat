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
	if (ctx->srcLen < sizeof(char*))
		return ERR_SRC_SHORT;
	// print text without buf
	const char* str = *(char**)ctx->psrc;
	size_t len = (NULL == str) ? 0 : strnlength(str, MAX_STR_LEN);
	if (ctx->dstLen < len + 2)
		return ERR_DST_SHORT;
	*ctx->pdst++ = '"';
	memcpy(ctx->pdst, str, len);
	ctx->pdst += len;
	*ctx->pdst++ = '"';
	len += 2;
	ctx->writed += len;
	ctx->readed += sizeof(char*);
	ctx->psrc += sizeof(char*);
	ctx->srcLen -= sizeof(char*);
	ctx->dstLen -= len;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringBinToStr(WalkContext_t* ctx)
{
	size_t sLen = ctx->psrc[0];
	if (ctx->srcLen < 1 + sLen)
		return ERR_SRC_SHORT;
	const char* found = memchr(&ctx->psrc[1], '\0', sLen);
	size_t pos = found - (char*)(&ctx->psrc[1]);
	if (pos < sLen)
		sLen = pos;
	if (ctx->dstLen < sLen + 2)
		return ERR_DST_SHORT;
	*ctx->pdst++ = '"';
	memcpy(ctx->pdst, ctx->psrc + 1, sLen);
	ctx->pdst += sLen;
	*ctx->pdst++ = '"';
	ctx->writed += 2 + sLen;
	ctx->readed += 1 + sLen;
	ctx->psrc += 1 + sLen;
	ctx->srcLen -= 1 + sLen;
	ctx->dstLen -= 2 + sLen;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringBinToBin(WalkContext_t* ctx)
{
	size_t sLen = ctx->psrc[0];
	size_t blength = 1;
	blength += sLen;
	if (ctx->srcLen < blength)
		return ERR_SRC_SHORT;
	if (ctx->dstLen < blength)
		return ERR_DST_SHORT;
	memcpy(ctx->pdst, ctx->psrc, blength);
	ctx->readed += blength;
	ctx->writed += blength;
	ctx->psrc += blength;
	ctx->srcLen -= blength;
	ctx->pdst += blength;
	ctx->dstLen -= blength;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringCBinToBin(WalkContext_t* ctx)
{
	if (ctx->srcLen < sizeof(char*))
		return ERR_SRC_SHORT;
	const char* str = *(char**)ctx->psrc;
	size_t len = (NULL == str) ? 0 : strnlength(str, MAX_STR_LEN);
	if (ctx->dstLen < len + 1)
		return ERR_DST_SHORT;
	ctx->pdst[0] = (uint8_t)len;
	ctx->pdst++;
	memcpy(ctx->pdst, str, len);
	ctx->pdst += len;
	ctx->writed += len + 1;
	ctx->readed += sizeof(char*);
	ctx->psrc += sizeof(char*);
	ctx->srcLen -= sizeof(char*);
	ctx->dstLen -= len + 1;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteCharAnyBinToStr(WalkContext_t* ctx)
{
	size_t actual_len = strnlength((const char*)ctx->psrc, ctx->srcLen);
	if (ctx->dstLen < actual_len + 2)
		return ERR_DST_SHORT;
	ctx->pdst[0] = '"';
	memcpy(ctx->pdst + 1, ctx->psrc, actual_len);
	ctx->pdst[actual_len + 2 - 1] = '"';
	ctx->readed += ctx->srcLen;
	ctx->writed += actual_len + 2;
	ctx->psrc += ctx->srcLen;
	ctx->srcLen -= ctx->srcLen;
	ctx->pdst += actual_len + 2;
	ctx->dstLen -= actual_len + 2;
	return ERR_NO;
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
int CBinToBin(PoType t, WalkContext_t* ctx)
{
	size_t readed, writed;
	switch (t)
	{
	case Struct:
	default: return ERR_WRONG_TYPE;
	case Int8: case UInt8:
		if (sizeof(uint8_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint8_t) > ctx->dstLen) return ERR_DST_SHORT;
		*ctx->pdst = *ctx->psrc;
		writed = readed = sizeof(uint8_t);
		break;
	case Half: case Int16: case UInt16:
		if (sizeof(uint16_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint16_t) > ctx->dstLen) return ERR_DST_SHORT;
		//memcpy(ctx->pdst, ctx->psrc, sizeof(uint16_t));
		*(uint16_t*)ctx->pdst = *(const uint16_t*)ctx->psrc;
		writed = readed = sizeof(uint16_t);
		break;
	case Single: case Int32: case UInt32:
		if (sizeof(uint32_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint32_t) > ctx->dstLen) return ERR_DST_SHORT;
		//memcpy(ctx->pdst, ctx->psrc, sizeof(uint32_t)); //
		*(uint32_t*)ctx->pdst = *(const uint32_t*)ctx->psrc;
		writed = readed = sizeof(uint32_t);
		break;
	case Double: case Int64: case UInt64:
		if (sizeof(uint64_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint64_t) > ctx->dstLen) return ERR_DST_SHORT;
		 //memcpy(ctx->pdst, ctx->psrc, sizeof(uint64_t));
		*(uint32_t*)ctx->pdst = *(const uint32_t*)ctx->psrc;//memcpy(dst, src, *r); break;
		*(uint32_t*)(ctx->pdst + 4) = *(const uint32_t*)(ctx->psrc + 4);
		writed = readed = sizeof(uint64_t);
		break;
	case Char:
		if (ctx->dstLen < ctx->srcLen) return ERR_DST_SHORT;
		memcpy(ctx->pdst, ctx->psrc, ctx->srcLen);
		writed = readed = ctx->srcLen;
		break;
	case String: return WriteStringCBinToBin(ctx);
	}//switch

	ctx->readed += readed;
	ctx->writed += writed;
	ctx->psrc += readed;
	ctx->srcLen -= readed;
	ctx->pdst += writed;
	ctx->dstLen -= writed;
	return 0;
}
//-----------------------------------------------------------------------------
int BinToBin(PoType t, WalkContext_t* ctx)
{
	size_t readed, writed;
	switch (t)
	{
	case Struct:
	default: return ERR_WRONG_TYPE;
	case Int8: case UInt8:
		if (sizeof(uint8_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint8_t) > ctx->dstLen) return ERR_DST_SHORT;
		*ctx->pdst = *ctx->psrc;
		writed = readed = sizeof(uint8_t);
		break;
	case Half: case Int16: case UInt16:
		if (sizeof(uint16_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint16_t) > ctx->dstLen) return ERR_DST_SHORT;
		//memcpy(ctx->pdst, ctx->psrc, sizeof(uint16_t));
		*(uint16_t*)ctx->pdst = *(const uint16_t*)ctx->psrc;
		writed = readed = sizeof(uint16_t);
		break;
	case Single: case Int32: case UInt32:
		if (sizeof(uint32_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint32_t) > ctx->dstLen) return ERR_DST_SHORT;
		//memcpy(ctx->pdst, ctx->psrc, sizeof(uint32_t)); //
		*(uint32_t*)ctx->pdst = *(const uint32_t*)ctx->psrc;
		writed = readed = sizeof(uint32_t);
		break;
	case Double: case Int64: case UInt64:
		if (sizeof(uint64_t) > ctx->srcLen) return ERR_SRC_SHORT;
		if (sizeof(uint64_t) > ctx->dstLen) return ERR_DST_SHORT;
		memcpy(ctx->pdst, ctx->psrc, sizeof(uint64_t));
		*(uint32_t*)ctx->pdst = *(const uint32_t*)ctx->psrc;//memcpy(dst, src, *r); break;
		*(uint32_t*)(ctx->pdst + 4) = *(const uint32_t*)(ctx->psrc + 4);
		writed = readed = sizeof(uint64_t);
		break;
	case Char:
		if (ctx->dstLen < ctx->srcLen)
		{
			return ERR_DST_SHORT;
		}
		memcpy(ctx->pdst, ctx->psrc, ctx->srcLen);
		writed = readed = ctx->srcLen;
		break;
	case String: return WriteStringBinToBin(ctx);
	}//switch

	ctx->readed += readed;
	ctx->writed += writed;
	ctx->psrc += readed;
	ctx->srcLen -= readed;
	ctx->pdst += writed;
	ctx->dstLen -= writed;
	return 0;
}
//-----------------------------------------------------------------------------
static int AnyBinToStr(PoType t, WalkContext_t* ctx, WriteStringFn WriteString)
{
	size_t readed = GetSizeOf(t);// переопределится для строки
	if (ctx->srcLen < readed)
	{
		return ERR_SRC_SHORT;
	}
	if (ctx->dstLen < 1)
	{
		return ERR_DST_SHORT;
	}
	size_t writed;
	switch (t)
	{
	case Struct:
	default: return ERR_WRONG_TYPE;
	case Int8:
		writed = Int32ToA((int8_t)ctx->psrc[0], (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint8_t);
		break;
	case UInt8:
		writed = UInt32ToA((uint8_t)ctx->psrc[0], (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint8_t);
		break;
	case Int16:
		writed = Int32ToA(*((int16_t*)ctx->psrc), (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint16_t);
		break;
	case UInt16:
		writed = UInt32ToA(*((uint16_t*)ctx->psrc), (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint16_t);
		break;
	case Int32:
		writed = Int32ToA(*((int32_t*)ctx->psrc), (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint32_t);
		break;
	case UInt32:
		writed = UInt32ToA(*((uint32_t*)ctx->psrc), (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint32_t);
		break;
	case Int64:
	{
		int64_t alignedVal;
		memcpy(&alignedVal, ctx->psrc, sizeof(int64_t));
		writed = Int64ToA(alignedVal, (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint64_t);
		break;
	}
	case UInt64:
	{
		uint64_t alignedVal;
		memcpy(&alignedVal, ctx->psrc, sizeof(uint64_t));
		writed = UInt64ToA(alignedVal, (char*)ctx->pdst, ctx->dstLen);
		if (0 == writed)
			return ERR_DST_SHORT;
		readed = sizeof(uint64_t);
		break;
	}
	case Half:
		//ctx->writed = sprintf_s(dst, ctx->dstLen, "%g", *((uint16_t*)src));
		return 0;
	case Single:
	{
		float alignedVal;
		memcpy(&alignedVal, ctx->psrc, sizeof(float));
		writed = xprint(ctx->pdst, ctx->dstLen, "%.9g", alignedVal);
		if (ctx->dstLen < writed)
			return ERR_DST_SHORT;
		readed = sizeof(float);
		break;
	}
	case Double:
	{
		double alignedVal;
		memcpy(&alignedVal, ctx->psrc, sizeof(double));
		writed = xprint(ctx->pdst, ctx->dstLen, "%.17g", alignedVal);
		if (ctx->dstLen < writed)
			return ERR_DST_SHORT;
		readed = sizeof(double);
		break;
	}
	case Char: return WriteCharAnyBinToStr(ctx);
	case String: return (*WriteString)(ctx);
		
	}//switch (t)

	ctx->readed += readed;
	ctx->writed += writed;
	ctx->psrc += readed;
	ctx->srcLen -= readed;
	ctx->pdst += writed;
	ctx->dstLen -= writed;
	return ERR_NO;
}
//-----------------------------------------------------------------------------
int CBinToStr(PoType t, WalkContext_t* ctx)
{
	return AnyBinToStr(t, ctx, WriteStringCBinToStr);
}
//-----------------------------------------------------------------------------
int BinToStr(PoType t, WalkContext_t* ctx)
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
