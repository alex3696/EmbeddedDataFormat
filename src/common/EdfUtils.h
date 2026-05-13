#ifndef EDFUTILS_H
#define EDFUTILS_H

#include "_pch.h"

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
#define LOG_ERR() printf("\n err: %d %s %s ", __LINE__, __FILE__, __FUNCTION__)
#endif

#ifndef LOG_ERRF
void Log_ErrF(const char* const fmt, ...);
#define LOG_ERRF(fmt, ...) Log_ErrF(fmt, __VA_ARGS__)
#endif

#endif //EDFUTILS_H
