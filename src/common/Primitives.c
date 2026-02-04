#include "_pch.h"
#include "Primitives.h"

//-----------------------------------------------------------------------------
typedef int (*WriteStringFn)(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w);
//-----------------------------------------------------------------------------
static int WriteStringCBinToStr(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	*r = GetSizeOf(String);
	if (srcLen < *r)
		return -1;
	// print text without buf
	const char* str = *(char**)src;
	size_t len = (NULL == str) ? 0 : strnlength(str, 0xFE);
	if (dstLen < len + 2)
		return 1;
	*w = 2 + len;
	*dst++ = '"';
	memcpy(dst, str, len);
	dst += len;
	*dst++ = '"';
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringBinToStr(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	*r = *w = 0;
	size_t sLen = src[0];
	if (srcLen < 1 + sLen)
		return -1;
	*r = 1 + sLen;
	const char* found = memchr(&src[1], '\0', sLen);
	size_t pos = found - (char*)(&src[1]);
	if (pos < sLen)
		sLen = pos;
	if (dstLen < sLen + 2)
		return 1;
	*dst++ = '"';
	memcpy(dst, src + 1, sLen);
	dst += sLen;
	*dst++ = '"';
	*w = 2 + sLen;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringBinToBin(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	size_t sLen = src[0];
	size_t blength = 1;
	*r = *w = 0;
	blength += sLen;
	if (srcLen < blength)
		return -1;
	if (dstLen < blength)
		return 1;
	memcpy(dst, src, blength);
	*r = *w = blength;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringCBinToBin(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	*r = GetSizeOf(String);
	if (srcLen < *r)
		return -1;
	const char* str = *(char**)src;
	size_t len = (NULL == str) ? 0 : strnlength(str, 0xFE) + 1;
	if (0 < len && '\0' == str[len - 1])// remove ending '\0'
		len--;
	if (dstLen < len + 1)
		return 1;
	(*dst) = (uint8_t)len;
	dst++;
	memcpy(dst, str, len);
	*w = len + 1;
	return 0;
}
//-----------------------------------------------------------------------------
static size_t xprint(const uint8_t* buf, size_t bufLen, char* format, ...)
{
	va_list arglist;
	va_start(arglist, format);
	int writed = vsnprintf((char*)buf, bufLen, format, arglist);
	va_end(arglist);
	if (writed && (size_t)writed == bufLen)
		return writed + 1;
	return writed;
}
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
static int AnyBinToBin(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w,
	WriteStringFn WriteString)
{
	*r = *w = GetSizeOf(t);
	if (srcLen < *r)
		return -1;
	if (dstLen < *w)
		return 1;
	switch (t)
	{
	case Struct:
	default: *r = *w = 0; return -2;
	case Int8:
	case UInt8:
	case UInt16:
	case UInt32:
	case UInt64:
	case Int16:
	case Int32:
	case Int64:
	case Half:
	case Single:
	case Double:
		memcpy(dst, src, *r);
		break;
	case Char:
		if (dstLen < srcLen)
		{
			*r = *w = 0;
			return 1;
		}
		*r = *w = srcLen;
		memcpy(dst, src, srcLen);
		return 0;
	case String: return (*WriteString)(src, srcLen, dst, dstLen, r, w);
	}//switch
	return 0;
}
//-----------------------------------------------------------------------------
int CBinToBin(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	return AnyBinToBin(t, src, srcLen, dst, dstLen, r, w, WriteStringCBinToBin);
}
//-----------------------------------------------------------------------------
int BinToBin(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	return AnyBinToBin(t, src, srcLen, dst, dstLen, r, w, WriteStringBinToBin);
}
//-----------------------------------------------------------------------------
static int AnyBinToStr(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w,
	WriteStringFn WriteString)
{
	*r = GetSizeOf(t);
	if (srcLen < *r)
	{
		*w = 0;
		return -1;
	}
	if (dstLen < 1)
	{
		*w = 0;
		return 1;
	}
	switch (t)
	{
	case Struct:
	default: *r = *w = 0; return -2;
	case Int8:
		*w = xprint(dst, dstLen, "%d", (int8_t)src[0]);
		return (dstLen < *w);
	case UInt8:
		*w = xprint(dst, dstLen, "%u", (uint8_t)src[0]);
		return (dstLen < *w);
	case Int16:
		*w = xprint(dst, dstLen, "%d", *((int16_t*)src));
		return (dstLen < *w);
	case UInt16:
		*w = xprint(dst, dstLen, "%u", *((uint16_t*)src));
		return (dstLen < *w);
	case Int32:
		*w = xprint(dst, dstLen, "%d", *((int32_t*)src));
		return (dstLen < *w);
	case UInt32:
		*w = xprint(dst, dstLen, "%lu", *((uint32_t*)src));
		return (dstLen < *w);
	case Int64:
	{
		int64_t alignedVal;
		memcpy(&alignedVal, src, sizeof(alignedVal));
		*w = xprint(dst, dstLen, "%lld", alignedVal);
	}
	return (dstLen < *w);
	case UInt64:
	{
		uint64_t alignedVal;
		memcpy(&alignedVal, src, sizeof(alignedVal));
		*w = xprint(dst, dstLen, "%llu", alignedVal);
	}
	return (dstLen < *w);
	case Half:
		//*w = sprintf_s(dst, dstLen, "%g", *((uint16_t*)src));
		return 0;
	case Single:
		*w = xprint(dst, dstLen, "%g", *((float*)src));
		return (dstLen < *w);
	case Double:
	{
		double alignedVal;
		memcpy(&alignedVal, src, sizeof(alignedVal));
		*w = xprint(dst, dstLen, "%.16g", alignedVal);
	}
	return (dstLen < *w);
	case Char:
		if (dstLen < (*w) + 2)
			return 1;
		*r = srcLen;
		*w = srcLen + 2;
		dst[0] = '"';
		memcpy(dst + 1, src, srcLen);
		dst[srcLen + 2 - 1] = '"';
		return 0;
	case String: return (*WriteString)(src, srcLen, dst, dstLen, r, w);
	}//switch (t)
}
//-----------------------------------------------------------------------------
int CBinToStr(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	return AnyBinToStr(t, src, srcLen, dst, dstLen, r, w, WriteStringCBinToStr);
}
//-----------------------------------------------------------------------------
int BinToStr(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	return AnyBinToStr(t, src, srcLen, dst, dstLen, r, w, WriteStringBinToStr);
}
//-----------------------------------------------------------------------------
int StreamWriteString(Stream_t* s, const char* str, size_t* writed)
{
	int err = 0;
	size_t len = str ? strnlength(str, 255) : 0;
	if ((err = StreamWrite(s, writed, &len, 1)) ||
		(err = StreamWrite(s, writed, str, len)))
		return err;
	return 0;
}
//-----------------------------------------------------------------------------
int StreamReadString(MemStream_t* tsrc, MemStream_t* tmem, char** ti)
{
	MemStream_t src = *tsrc;
	MemStream_t mem = *tmem;
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
			uint8_t* pStrEnd = NULL;
			if ((err = MemAlloc(&mem, 1, (void**)&pStrEnd)))
				return ERR_DST_SHORT;
			//pStrEnd[0] = '\0';
		}
	}
	*ti = pstr;
	*tsrc = src;
	*tmem = mem;
	return 0;
}
//-----------------------------------------------------------------------------
