#ifndef EDFUTILS_H
#define EDFUTILS_H

#include "_pch.h"
#include "stdlib.h"

// !!! use UTF-8 (no BOM) as source files in MSVC strlen
// check:	assert(8 == strlen("тест"));
// https://learn.microsoft.com/en-us/cpp/build/reference/utf-8-set-source-and-executable-character-sets-to-utf-8?view=msvc-170
// https://habr.com/ru/articles/731614/
// https://stackoverflow.com/questions/58580912/msvc-utf8-string-encoding-uses-incorrect-code-points
// https://stackoverflow.com/questions/1660712/specification-of-source-charset-encoding-in-msvc-like-gcc-finput-charset-ch

size_t strnlength(const char* s, size_t n);
int CallStackSize(void);

uint16_t MbCrc16acc(const void* d, size_t len, uint16_t crc);
#define MbCrc16(data,len) MbCrc16acc((data),(len),0xFFFF)

#define FIELD_SIZEOF(t, f) (sizeof(((t*)0)->f))
#define FIELD_ITEMS_COUNT(t, f) FIELD_SIZEOF(t, f)/(sizeof(((t*)0)->f[0]))

#ifndef MAX
#define MAX(x, y) (((x) > (y)) ? (x) : (y))
#endif 

#ifndef MIN
#define MIN(x, y) (((x) < (y)) ? (x) : (y))
#endif 

#ifndef UNUSED
//#define UNUSED(x) ((x)=(x))
#define UNUSED(x) (void)(x);
#endif 

#ifndef LOG_ERR
#define LOG_ERR() fprintf(stderr, "Error: %d %s %s ", __LINE__, __FUNCTION__, __FILE__)
#define LOG_ERR_CODE(message, code) fprintf(stderr, "Error [%d] %s: %d %s %s ", code, message, __LINE__, __FUNCTION__, __FILE__)
#endif

#define runtime_assert0(condition)                if (!(condition)) { LOG_ERR(); abort(); } 
#define runtime_assert(condition, message, code)  if (!(condition)) { LOG_ERR_CODE(message, code); abort(); } 

#endif //EDFUTILS_H
