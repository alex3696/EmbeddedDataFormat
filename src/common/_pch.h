#ifndef PCH_H
#define PCH_H

#ifdef _MSC_VER
#pragma warning(disable : 5045)
#endif

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

#define EDF_VERSMAJOR	0		
#define EDF_VERSMINOR	3
#define EDF_ENCODING	65001	//UTF-8

#define MIN_BLOCK_SIZE	256
#define MAX_BLOCK_SIZE	4096
#define MAX_STR_LEN		255

#define EDF_CONSTSTR(val) &(const char*){val}

#define ERR_EOF -1
#define ERR_NO 0

// error codes do not overlap with errno

#define ERR_BASE 1000
#define ERR_SRC_SHORT			ERR_BASE+1
#define ERR_DST_SHORT			ERR_BASE+2
#define ERR_WRONG_TYPE			ERR_BASE+3
#define ERR_WRONG_PARAMETERS	ERR_BASE+4
#define ERR_FREAD				ERR_BASE+5
#define ERR_FWRITE				ERR_BASE+6
#define ERR_FN_NOT_EXIST		ERR_BASE+7

#define ERR_BLK ERR_BASE+100
#define ERR_BLK_WRONG_TYPE		ERR_BLK+20
#define ERR_BLK_WRONG_SIZE		ERR_BLK+21
#define ERR_BLK_WRONG_CRC		ERR_BLK+22
#define ERR_BLOCK_SIZE_LARGE    ERR_BLK+23

#endif //PCH_H
