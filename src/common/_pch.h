#ifndef PCH_H
#define PCH_H

#ifdef __cplusplus
#include <cstdint>
#include <cstdio>
#include <cstring>
#else
#include "stdint.h"
#include "stdio.h"
#include "string.h"
#endif

//#include "windows.h"
//#include "uchar.h"
//#include "assert.h"
#include "memory.h"
#include "stdarg.h"

#include "edf_cfg.h"

int CallStackSize();

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

size_t strnlength(const char* s, size_t n);

#define ERR_NO 0
#define ERR_SRC_SHORT -1
#define ERR_DST_SHORT 1
#define ERR_WRONG_TYPE 2

#define ERR_FN_NOT_EXIST -2

#define ERR_BLK_WRONG_TYPE -20
#define ERR_BLK_WRONG_SEQ -21
#define ERR_BLK_WRONG_SIZE -22
#define ERR_BLK_WRONG_CRC -23



#endif //PCH_H