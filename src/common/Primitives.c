#include "_pch.h"
#include "edf.h"

void putchar_(char character)
{
	// заглушка 
	// (void)character;
}
// Таблица пар символов от "00" до "99"
static const char DigitPairs[] =
"0001020304050607080910111213141516171819"
"2021222324252627282930313233343536373839"
"4041424344454647484950515253545556575859"
"6061626364656667686970717273747576777879"
"8081828384858687888990919293949596979899";

static inline size_t internal_utoa(uint64_t uval, int is_negative, char* dst, size_t dstLen) {
	char buf[24]; // Буфер с запасом под uint64 + знак + '\0'
	char* p = &buf[23];
	*p = '\0';

	// Основной цикл обработки по 2 цифры
	while (uval >= 100) {
		uint32_t rem = (uint32_t)(uval % 100);
		uval /= 100;
		p -= 2;
		p[0] = DigitPairs[rem * 2];
		p[1] = DigitPairs[rem * 2 + 1];
	}
	// Обработка остатка
	if (uval >= 10) {
		p -= 2;
		p[0] = DigitPairs[uval * 2];
		p[1] = DigitPairs[uval * 2 + 1];
	}
	else {
		*--p = (char)('0' + uval);
	}
	// Добавляем минус, если нужно
	if (is_negative) {
		*--p = '-';
	}
	size_t len = &buf[23] - p;
	if (len >= dstLen) {
		return 0; // Буфер слишком мал
	}
	memcpy(dst, p, len);
	dst[len] = '\0';
	return len;
}
// обёртка для знаковых чисел
static size_t fast_itoa_table(int64_t val, char* dst, size_t dstLen) {
	int is_negative = 0;
	uint64_t uval;

	if (val < 0) {
		is_negative = 1;
		uval = (val == INT64_MIN) ? (uint64_t)INT64_MAX + 1 : (uint64_t)(-val);
	}
	else {
		uval = (uint64_t)val;
	}

	return internal_utoa(uval, is_negative, dst, dstLen);
}
// обёртка для беззнаковых чисел
static size_t fast_utoa_table(uint64_t val, char* dst, size_t dstLen) {
	return internal_utoa(val, 0, dst, dstLen);
}
//-----------------------------------------------------------------------------
typedef int (*WriteStringFn)(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w);
//-----------------------------------------------------------------------------
static int WriteStringCBinToStr(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	*r = sizeof(char*);
	if (srcLen < *r)
		return ERR_SRC_SHORT;
	// print text without buf
	const char* str = *(char**)src;
	size_t len = (NULL == str) ? 0 : strnlength(str, MAX_STR_LEN);
	if (dstLen < len + 2)
		return ERR_DST_SHORT;
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
		return ERR_SRC_SHORT;
	*r = 1 + sLen;
	const char* found = memchr(&src[1], '\0', sLen);
	size_t pos = found - (char*)(&src[1]);
	if (pos < sLen)
		sLen = pos;
	if (dstLen < sLen + 2)
		return ERR_DST_SHORT;
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
		return ERR_SRC_SHORT;
	if (dstLen < blength)
		return ERR_DST_SHORT;
	memcpy(dst, src, blength);
	*r = *w = blength;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteStringCBinToBin(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	*r = sizeof(char*);
	if (srcLen < *r)
		return ERR_SRC_SHORT;
	const char* str = *(char**)src;
	size_t len = (NULL == str) ? 0 : strnlength(str, MAX_STR_LEN);
	if (dstLen < len + 1)
		return ERR_DST_SHORT;
	(*dst) = (uint8_t)len;
	dst++;
	memcpy(dst, str, len);
	*w = len + 1;
	return 0;
}
//-----------------------------------------------------------------------------
static int WriteCharAnyBinToStr(const uint8_t* src, size_t srcLen, uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w)
{
	size_t actual_len = strnlength((const char*)src, srcLen);
	if (dstLen < actual_len + 2)
		return ERR_DST_SHORT;
	*r = srcLen;
	*w = actual_len + 2;
	dst[0] = '"';
	memcpy(dst + 1, src, actual_len);
	dst[actual_len + 2 - 1] = '"';
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
//-----------------------------------------------------------------------------
static int AnyBinToBin(PoType t,
	const uint8_t* src, size_t srcLen,
	uint8_t* dst, size_t dstLen,
	size_t* r, size_t* w,
	WriteStringFn WriteString)
{
	*r = *w = GetSizeOf(t);// переопределится для строки
	if (srcLen < *r)
		return ERR_SRC_SHORT;
	if (dstLen < *w)
		return ERR_DST_SHORT;
	switch (t)
	{
	case Struct:
	default: *r = *w = 0; return ERR_WRONG_TYPE;
	case Int8: 
	case UInt8: *dst = *src; break;
	case Half:
	case Int16:
	case UInt16: *(uint16_t*)dst = *(const uint16_t*)src; break;
	case Single:
	case Int32:
	case UInt32: *(uint32_t*)dst = *(const uint32_t*)src; break;
	case Double:
	case Int64:
	case UInt64: *(uint64_t*)dst = *(const uint64_t*)src; break;
		//memcpy(dst, src, *r); break;
	case Char:
		if (dstLen < srcLen)
		{
			*r = *w = 0;
			return ERR_DST_SHORT;
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
	*r = GetSizeOf(t);// переопределится для строки
	if (srcLen < *r)
	{
		*w = 0;
		return ERR_SRC_SHORT;
	}
	if (dstLen < 1)
	{
		*w = 0;
		return ERR_DST_SHORT;
	}
	switch (t)
	{
	case Struct:
	default: *r = *w = 0; return ERR_WRONG_TYPE;
	case Int8:
		*w = fast_itoa_table((int8_t)src[0], (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	case UInt8:
		*w = fast_utoa_table((uint8_t)src[0], (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	case Int16:
		*w = fast_itoa_table(*((int16_t*)src), (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	case UInt16:
		*w = fast_utoa_table(*((uint16_t*)src), (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	case Int32:
		*w = fast_itoa_table(*((int32_t*)src), (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	case UInt32:
		*w = fast_utoa_table(*((uint32_t*)src), (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	case Int64:
	{
		int64_t alignedVal;
		memcpy(&alignedVal, src, sizeof(int64_t));
		*w = fast_itoa_table(alignedVal, (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	}
	case UInt64:
	{
		uint64_t alignedVal;
		memcpy(&alignedVal, src, sizeof(uint64_t));
		*w = fast_utoa_table(alignedVal, (char*)dst, dstLen);
		return (*w == 0) ? ERR_DST_SHORT : ERR_NO;
	}
	case Half:
		//*w = sprintf_s(dst, dstLen, "%g", *((uint16_t*)src));
		return 0;
	case Single:
	{
		float alignedVal;
		memcpy(&alignedVal, src, sizeof(float));
		*w = xprint(dst, dstLen, "%.9g", alignedVal);
		return (dstLen < *w) ? ERR_DST_SHORT : ERR_NO;
	}
	case Double:
	{
		double alignedVal;
		memcpy(&alignedVal, src, sizeof(double));
		*w = xprint(dst, dstLen, "%.17g", alignedVal);
		return (dstLen < *w) ? ERR_DST_SHORT : ERR_NO;
	}
	case Char: return WriteCharAnyBinToStr(src, srcLen, dst, dstLen, r, w);
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
	*tsrc = src;
	*tmem = mem;
	return 0;
}
//-----------------------------------------------------------------------------
