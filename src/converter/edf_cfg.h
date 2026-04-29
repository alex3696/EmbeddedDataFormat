#ifndef EDFCFG_H
#define EDFCFG_H

//-----------------------------------------------------------------------------
#ifdef __cplusplus
extern "C" {
#endif
//-----------------------------------------------------------------------------

#ifndef BLOCK_SIZE
	#ifdef _MSC_VER
		#define BLOCK_SIZE 512
	#elif
		#define BLOCK_SIZE 256
	#endif
#endif

//#define LOG_ERR
//#define LOG_ERRF
//#define STREAM_BUF_SIZE 64

// !!! use UTF-8 (no BOM) as source files in MSVC strlen
// check:	assert(8 == strlen("тест"));
// https://learn.microsoft.com/en-us/cpp/build/reference/utf-8-set-source-and-executable-character-sets-to-utf-8?view=msvc-170
// https://habr.com/ru/articles/731614/
// https://stackoverflow.com/questions/58580912/msvc-utf8-string-encoding-uses-incorrect-code-points
// https://stackoverflow.com/questions/1660712/specification-of-source-charset-encoding-in-msvc-like-gcc-finput-charset-ch


//-----------------------------------------------------------------------------
#ifdef __cplusplus
}
#endif
//-----------------------------------------------------------------------------

#include "edf.h"

#endif
